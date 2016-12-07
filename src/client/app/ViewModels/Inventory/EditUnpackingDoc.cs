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
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditUnpackingDoc : BaseScreen2
	{
		private string Name;

		private EditUnpackingDoc()
		{
			Lines = new ReactiveCollection<UnpackingDocLine>();
			Session.FlushMode = FlushMode.Never;
			Name = User?.FullName ?? "";
		}

		public EditUnpackingDoc(UnpackingDoc doc)
			: this()
		{
			DisplayName = "Новый документ Распаковка";
			InitDoc(doc);
			Lines.AddRange(doc.Lines);
		}

		public EditUnpackingDoc(uint id)
			: this()
		{
			DisplayName = "Редактирование документа Распаковка";
			InitDoc(Session.Load<UnpackingDoc>(id));
			Lines.AddRange(Doc.Lines);
		}

		public UnpackingDoc Doc { get; set; }
		public ReactiveCollection<UnpackingDocLine> Lines { get; set; }
		public NotifyValue<UnpackingDocLine> CurrentLine { get; set; }
		public NotifyValue<bool> CanAdd { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanPost { get; set; }
		public NotifyValue<bool> CanUnPost { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Doc.Id == 0)
				Doc.Address = Address;
		}

		protected override void OnDeactivate(bool close)
		{
			Save();
			base.OnDeactivate(close);
		}

		private void InitDoc(UnpackingDoc doc)
		{
			Doc = doc;
			var docStatus = Doc.ObservableForProperty(x => x.Status, skipInitial: false);
			var editOrDelete = docStatus
				.CombineLatest(CurrentLine, (x, y) => y != null && x.Value == DocStatus.NotPosted);
			editOrDelete.Subscribe(CanDelete);
			docStatus.Subscribe(x => CanAdd.Value = x.Value == DocStatus.NotPosted);
			docStatus.Select(x => x.Value == DocStatus.NotPosted).Subscribe(CanPost);
			docStatus.Select(x => x.Value == DocStatus.Posted).Subscribe(CanUnPost);
		}

		public IEnumerable<IResult> Add()
		{
			var search = new StockSearch();
			yield return new DialogResult(search, resizable: true);
			var edit = new EditStock(search.CurrentItem)
			{
				EditMode = EditStock.Mode.EditQuantity
			};
			yield return new DialogResult(edit);
			// откуда брать кратность?
			var line = new UnpackingDocLine(Session.Load<Stock>(edit.Stock.Id), 10);
			Lines.Add(line);
			Doc.Lines.Add(line);
			Doc.UpdateStat();
			Save();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;
			Doc.DeleteLine(CurrentLine.Value);
			Lines.Remove(CurrentLine.Value);
			Doc.UpdateStat();
			Save();
		}

		public void Post()
		{
			Doc.Post();
			Save();
		}

		public void UnPost()
		{
			Doc.UnPost();
			Save();
		}

		private void Save()
		{
			if (Doc.Id == 0)
				Session.Save(Doc);
			else
				Session.Update(Doc);
			Session.Flush();
			Bus.SendMessage(nameof(UnpackingDoc), "db");
			Bus.SendMessage(nameof(Stock), "db");
		}
	}
}