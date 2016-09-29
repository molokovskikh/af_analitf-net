using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditReassessmentDoc : BaseScreen2
	{
		private EditReassessmentDoc()
		{
			Lines = new ReactiveCollection<ReassessmentLine>();
			Session.FlushMode = FlushMode.Never;
		}

		public EditReassessmentDoc(ReassessmentDoc doc)
			: this()
		{
			DisplayName = "Новая переоценка";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditReassessmentDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование переоценки";
			InitDoc(Session.Load<ReassessmentDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public ReassessmentDoc Doc { get; set; }

		public NotifyValue<bool> IsDocOpen { get; set; }
		public ReactiveCollection<ReassessmentLine> Lines { get; set; }
		public NotifyValue<ReassessmentLine> CurrentLine { get; set; }

		public NotifyValue<bool> CanAddLine { get; set; }
		public NotifyValue<bool> CanDeleteLine { get; set; }
		public NotifyValue<bool> CanEditLine { get; set; }
		public NotifyValue<bool> CanSave { get; set; }
		public NotifyValue<bool> CanCloseDoc { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Doc.Id == 0)
				Doc.Address = Address;
		}

		private void InitDoc(ReassessmentDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DocStatus.Opened);
			editOrDelete.Subscribe(CanEditLine);
			editOrDelete.Subscribe(CanDeleteLine);
			docStatus.Subscribe(x => CanAddLine.Value = x.Value == DocStatus.Opened);
			docStatus.Select(x => x.Value == DocStatus.Opened).Subscribe(IsDocOpen);
			docStatus.Select(x => x.Value == DocStatus.Opened).Subscribe(CanCloseDoc);
			docStatus.Select(x => x.Value == DocStatus.Opened).Subscribe(CanSave);
		}

		public IEnumerable<IResult> AddLine()
		{
			var search = new StockSearch();
			yield return new DialogResult(search);
			var srcStock = Session.Load<Stock>(search.CurrentItem.Value.Id);
			var dstStock = srcStock.Copy();
			var edit = new EditStock(dstStock) {
				EditMode = EditStock.Mode.EditRetailCostAndQuantity
			};
			yield return new DialogResult(edit);
			var line = new ReassessmentLine(srcStock, dstStock);
			Lines.Add(line);
			Doc.Lines.Add(line);
			Doc.UpdateStat();
		}

		public void DeleteLine()
		{
			if (!CanDeleteLine)
				return;
			Doc.DeleteLine(CurrentLine.Value);
			Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EditLine()
		{
			if (!CanEditLine)
				yield break;
			var line = CurrentLine.Value;
			var stock = StatelessSession.Get<Stock>(line.SrcStock.Id);
			stock.Quantity = line.DstStock.Quantity;
			var edit = new EditStock(stock) {
				EditMode = EditStock.Mode.EditRetailCostAndQuantity
			};
			yield return new DialogResult(edit);
			line.RetailCost = edit.Stock.RetailCost;
			line.UpdateQuantity(edit.Stock.Quantity);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EnterLine()
		{
			return EditLine();
		}

		public void CloseDoc()
		{
			Doc.Close(Session);
			Save();
		}

		public void Save()
		{
			if (!IsValide(Doc))
				return;
			Session.Save(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(ReassessmentDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}
	}
}