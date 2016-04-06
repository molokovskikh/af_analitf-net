using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using Dapper;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaybillDetails : BaseScreen
	{
		private uint id;
		private PriceTag priceTag;

		//для восстановления состояния
		public WaybillDetails(long id)
			: this((uint)id)
		{
		}

		public WaybillDetails(uint id)
		{
			DisplayName = "Детализация накладной";
			this.id = id;
			CurrentLine = new NotifyValue<object>();
			CurrentWaybillLine = CurrentLine.OfType<WaybillLine>().ToValue();
			CurrentTax = new NotifyValue<ValueDescription<int?>>();
			Rounding = new NotifyValue<Rounding>(Models.Rounding.To0_10);
			CurrentOrderLine = new NotifyValue<SentOrderLine>();
			Lines = new NotifyValue<ListCollectionView>();

			Rounding.Changed()
				.Merge(Settings.Changed())
				.Subscribe(v => Calculate());
			CurrentTax.Subscribe(v => {
				if (Lines.Value == null)
					return;
				if (v == null || v.Value == -1)
					Lines.Value.Filter = null;
				else
					Lines.Value.Filter = o => ((WaybillLine)o).Nds == v.Value;
			});
			Lines.Select(v => v == null
				? Observable.Empty<EventPattern<NotifyCollectionChangedEventArgs>>()
				: v.ToCollectionChanged())
			.Switch()
			.Subscribe(e => {
				if (e.EventArgs.Action == NotifyCollectionChangedAction.Remove) {
					e.EventArgs.OldItems.OfType<WaybillLine>().Each(l => Waybill.RemoveLine(l));
				}
				else if (e.EventArgs.Action == NotifyCollectionChangedAction.Add) {
					e.EventArgs.NewItems.OfType<WaybillLine>().Each(l => Waybill.AddLine(l));
				}
			});
			OrderLines = CurrentWaybillLine
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Select(v => {
					if (v == null)
						return new List<SentOrderLine>();
					var lineId = v.Id;
					var orderLineIds = Session.Query<WaybillOrder>().Where(l => l.DocumentLineId == lineId)
						.Select(l => (uint?)l.OrderLineId)
						.ToArray();
					var line = Session.Query<SentOrderLine>().FirstOrDefault(l => orderLineIds.Contains(l.ServerId));
					if (line != null) {
						CurrentOrderLine.Value = line;
						line.Order.Lines.Each(l => l.Configure(User));
						return line.Order.Lines.OrderBy(l => l.ProductSynonym).ToList();
					}
					return new List<SentOrderLine>();
				})
				.ToValue(CloseCancellation);

			EmptyLabelVisibility = OrderLines
				.Select(v => v == null || v.Count == 0 ? Visibility.Visible : Visibility.Collapsed)
				.ToValue();
			OrderDetailsVisibility = EmptyLabelVisibility
				.Select(v => v == Visibility.Collapsed ? Visibility.Visible :  Visibility.Collapsed)
				.ToValue();
			SessionValue(Rounding, nameof(Rounding));
		}

		public Waybill Waybill { get; set; }
		//datagrid для обозначения новой строки использует специальный внутренний объект
		//что бы избежать ошибок в тестах
		public NotifyValue<object> CurrentLine { get; set; }
		public NotifyValue<ListCollectionView> Lines { get; set; }
		public NotifyValue<WaybillLine> CurrentWaybillLine { get; set; }
		public List<ValueDescription<int?>> Taxes { get; set; }
		public NotifyValue<ValueDescription<int?>> CurrentTax { get; set; }

		public NotifyValue<List<SentOrderLine>> OrderLines { get; set; }
		public NotifyValue<SentOrderLine> CurrentOrderLine { get; set; }
		public NotifyValue<Visibility> OrderDetailsVisibility { get; set; }
		public NotifyValue<Visibility> EmptyLabelVisibility { get; set; }
		public NotifyValue<bool> IsRejectVisible { get; set; }
		public NotifyValue<Reject> Reject { get; set; }
		public NotifyValue<Rounding> Rounding { get; set; }

		private void Calculate()
		{
			//в случае если мы восстановили значение из сессии
			if (Waybill == null)
				return;
			Settings.Value.Rounding = Rounding.Value;
			Waybill.Calculate(Settings.Value);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Session == null)
				return;

			Waybill = Session.Load<Waybill>(id);

			Calculate();

			Lines.Value = new ListCollectionView(Waybill.Lines.OrderBy(l => l.Product).ToList());
			Taxes = new List<ValueDescription<int?>> {
				new ValueDescription<int?>("Все", -1),
			};
			Taxes.AddRange(Lines.Value.Cast<WaybillLine>().Select(l => l.Nds).Distinct()
				.OrderBy(t => t)
				.Select(t => new ValueDescription<int?>(((object)t ?? "Нет значения").ToString(), t)));
			CurrentTax.Value = Taxes.FirstOrDefault();
			Reject = CurrentWaybillLine
				.Throttle(Consts.ScrollLoadTimeout, Scheduler)
				.Select(v => RxQuery(s => {
					if (v?.RejectId == null)
						return null;
					return s.Get<Reject>(v.RejectId.Value);
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.ToValue(CloseCancellation);
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection))
				.ObserveOn(UiScheduler)
				.Subscribe(x => priceTag = x);
			IsRejectVisible = Reject.Select(r => r != null).ToValue();
		}

		public IResult PrintRackingMap()
		{
			return new DialogResult(new PrintPreviewViewModel {
				DisplayName = "Стеллажная карта",
				Document = new RackingMapDocument(Waybill, PrintableLines(), Settings.Value).Build()
			}, fullScreen: true);
		}

		public IResult PrintPriceTags()
		{
			return new DialogResult(new PrintPreviewViewModel {
				DisplayName = "Ценники",
				Document = new PriceTagDocument(Waybill, PrintableLines(), Settings.Value, priceTag).Build()
			}, fullScreen: true);
		}

		public IEnumerable<IResult> PrintRegistry()
		{
			var doc = new RegistryDocument(Waybill, PrintableLines());
			return Preview("Реестр", doc);
		}

		public IEnumerable<IResult> PrintWaybill()
		{
			return Preview("Накладная", new WaybillDocument(Waybill, PrintableLines()));
		}

		public IEnumerable<IResult> PrintInvoice()
		{
			return Preview("Счет-фактура", new InvoiceDocument(Waybill));
		}

		private IList<WaybillLine> PrintableLines()
		{
			//в случае редактирование пользовательской накладной в коллекции будет NamedObject
			return Lines.Value.OfType<WaybillLine>().Where(l => l.Print).ToList();
		}

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null) {
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}

		public IResult ExportWaybill()
		{
			var columns = new[] {
				"№ пп",
				"Наименование и краткая характеристика товара",
				"Серия товара Сертификат",
				"Срок годности",
				"Производитель",
				"Цена без НДС, руб",
				"Затребован.колич.",
				"Опт. надб. %",
				"Отпуск. цена пос-ка без НДС, руб",
				"НДС пос-ка, руб",
				"Отпуск. цена пос-ка с НДС, руб",
				"Розн. торг. надб. %",
                "Розн. торг. надб. руб",
                "Розн. цена за ед., руб",
				"Кол-во",
				"Розн. сумма, руб"
			};
			var items = PrintableLines().Select((l, i) => new object[] {
				i + 1,
				l.Product,
				$"{l.SerialNumber} {l.Certificates}",
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.Quantity,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.Nds,
				l.SupplierCost,
				l.RetailMarkup,
                l.RetailMarkupInRubles,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			var book = ExcelExporter.ExportTable(columns, items, 8);
			var sheet = book.GetSheetAt(0);
			sheet.CreateRow(1).CreateCell(6).SetCellValue(
				$"Наименование организации: Сотрудник {Waybill.WaybillSettings.FullName}");
			var row = sheet.CreateRow(2);
			row.CreateCell(3).SetCellValue("Отдел:");
			row.CreateCell(4).SetCellValue("_______________________________________");

			row = sheet.CreateRow(3);
			row.CreateCell(0).SetCellValue("Требование №");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("Накладная №");
			row.CreateCell(6).SetCellValue("_______________________");

			row = sheet.CreateRow(4);
			row.CreateCell(1).SetCellValue("от \"___\"_________________20___г");
			row.CreateCell(6).SetCellValue("от \"___\"_________________20___г");

			row = sheet.CreateRow(5);
			row.CreateCell(0).SetCellValue("Кому: Аптечный пункт");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("Через кого");
			row.CreateCell(6).SetCellValue("_______________________");

			row = sheet.CreateRow(6);
			row.CreateCell(0).SetCellValue("Основание отпуска");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("Доверенность №_____");
			row.CreateCell(6).SetCellValue("от \"___\"_________________20___г");
			return ExcelExporter.Export(book);
		}

		public IResult ExportRegistry()
		{
			var columns = new[] {
				"№ пп",
				"Наименование",
				"Серия товара",
				"Срок годности",
				"Производитель",
				"Цена без НДС, руб",
				"Цена ГР, руб",
				"Опт. надб. %",
				"Отпуск. цена пос-ка без НДС, руб",
				"НДС пос-ка, руб",
				"Отпуск. цена пос-ка с НДС, руб",
				"Розн. торг. надб. %",
                "Розн. торг. надб. руб",
				"Розн. цена за ед., руб",
				"Кол-во",
				"Розн. сумма, руб"
			};
			var items = PrintableLines().Select((l, i) => new object[] {
				i + 1,
				l.Product,
				l.SerialNumber,
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.RegistryCost,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.Nds,
				l.SupplierCost,
				l.RetailMarkup,
                l.RetailMarkupInRubles,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			var book = ExcelExporter.ExportTable(columns, items, 5);
			var sheet = book.GetSheetAt(0);
			sheet.CreateRow(1).CreateCell(6).SetCellValue("Реестр");
			sheet.CreateRow(2).CreateCell(3)
				.SetCellValue("розничных цен на лекарственные средства и изделия медицинского назначения,");
			sheet.CreateRow(3).CreateCell(3)
				.SetCellValue(
					$"полученные от {Waybill.SupplierName}-по счету (накладной) №{Waybill.ProviderDocumentId} от {Waybill.DocumentDate:d}");
			return ExcelExporter.Export(book);
		}

		public override IEnumerable<IResult> Download(Loadable loadable)
		{
			var supplier = ((WaybillLine)loadable).Waybill.SafeSupplier;
			if (supplier == null || !supplier.HaveCertificates) {
				yield return new MessageResult("Данный поставщик не предоставляет сертификаты в АналитФармация." +
					"\r\nОбратитесь к поставщику.",
					MessageResult.MessageType.Warning);
				yield break;
			}
			base.Download(loadable);
		}

		public void HideReject()
		{
			IsRejectVisible.Value = false;
		}

#if DEBUG
		public override object[] GetRebuildArgs()
		{
			return new object[] {
				id
			};
		}
#endif
	}
}