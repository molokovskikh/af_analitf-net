using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditWriteoffDoc : BaseScreen2
	{
		private EditWriteoffDoc()
		{
			Lines = new ReactiveCollection<WriteoffLine>();
			Session.FlushMode = FlushMode.Never;
		}

		public EditWriteoffDoc(WriteoffDoc doc)
			: this()
		{
			DisplayName = "Новое списание";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditWriteoffDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование списания";
			InitDoc(Session.Load<WriteoffDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public WriteoffDoc Doc { get; set; }

		public NotifyValue<bool> IsDocOpen { get; set; }
		public ReactiveCollection<WriteoffLine> Lines { get; set; }
		public NotifyValue<WriteoffLine> CurrentLine { get; set; }

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

		private void InitDoc(WriteoffDoc doc)
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
			var edit = new EditStock(search.CurrentItem) {
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			var line = new WriteoffLine(edit.Stock);
			Lines.Add(line);
			Doc.Lines.Add(line);
			Doc.UpdateStat();
		}

		public void DeleteLine()
		{
			Lines.Remove(CurrentLine.Value);
			Doc.Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
		}

		public IEnumerable<IResult> EditLine()
		{
			//if (!CanEditLine)
			//	yield break;
			//var line = new EditInventoryLine(CurrentLine.Value);
			//yield return new DialogResult(line);
			//Doc.UpdateStat();
			yield break;
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
			Session.Save(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(WriteoffDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}
	}
}