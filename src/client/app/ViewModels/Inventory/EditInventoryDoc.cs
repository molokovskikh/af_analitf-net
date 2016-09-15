using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditInventoryDoc : BaseScreen2
	{
		private EditInventoryDoc()
		{
			Lines = new ReactiveCollection<InventoryDocLine>();
			Session.FlushMode = FlushMode.Never;
		}

		public EditInventoryDoc(InventoryDoc doc)
			: this()
		{
			DisplayName = "Новая инвентаризация";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditInventoryDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование инвентаризации";
			InitDoc(Session.Load<InventoryDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public InventoryDoc Doc { get; set; }

		public NotifyValue<bool> IsDocOpen { get; set; }
		public ReactiveCollection<InventoryDocLine> Lines { get; set; }
		public NotifyValue<InventoryDocLine> CurrentLine { get; set; }

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

		private void InitDoc(InventoryDoc doc)
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
			var product = new ProductSearch();
			yield return new DialogResult(product);
			var line = new InventoryDocLine(product.CurrentItem);
			var edit = new EditInventoryLine(line);
			yield return new DialogResult(edit);
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
			if (!CanEditLine)
				yield break;
			var line = new EditInventoryLine(CurrentLine.Value);
			yield return new DialogResult(line);
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
			Session.Save(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(InventoryDoc), "db");
		}
	}
}