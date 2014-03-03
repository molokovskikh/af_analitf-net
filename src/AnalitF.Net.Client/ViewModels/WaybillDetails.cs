using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate.Linq;
using NHibernate.Util;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaybillDetails : BaseScreen
	{
		private uint id;

		public WaybillDetails(uint id)
		{
			DisplayName = "Детализация накладной";
			this.id = id;
			CurrentTax = new NotifyValue<ValueDescription<int?>>();
			RoundToSingleDigit = new NotifyValue<bool>(true);
			RoundToSingleDigit.Changed()
				.Merge(Settings.Changed())
				.Subscribe(v => Calculate());
			Lines = new NotifyValue<IList<WaybillLine>>(() => (Waybill ?? new Waybill())
				.Lines
				.Where(l => l.Nds == CurrentTax.Value.Value || CurrentTax.Value.Value == -1)
				.ToList(), CurrentTax);
		}

		private void Calculate()
		{
			Waybill.RoundTo1 = RoundToSingleDigit.Value;
			Waybill.Calculate(Settings.Value);
		}

		public Waybill Waybill { get; set; }
		public NotifyValue<IList<WaybillLine>> Lines { get; set; }
		public List<ValueDescription<int?>> Taxes { get; set; }
		public NotifyValue<ValueDescription<int?>> CurrentTax { get; set; }
		public NotifyValue<bool> RoundToSingleDigit { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Waybill = Session.Load<Waybill>(id);

			Calculate();

			Lines.Value = Waybill.Lines;
			Taxes = new List<ValueDescription<int?>> {
				new ValueDescription<int?>("Все", -1),
			};
			Taxes.AddRange(Lines.Value.Select(l => l.Nds).Distinct()
				.OrderBy(t => t)
				.Select(t => new ValueDescription<int?>(((object)t ?? "Нет значения").ToString(), t)));
			CurrentTax.Value = Taxes.FirstOrDefault();
		}

		public IResult PrintRackingMap()
		{
			return new DialogResult(new PrintPreviewViewModel {
				DisplayName = "Стеллажная карта",
				Document = new RackingMapDocument(Waybill, PrintableLines(), Settings.Value).Build()
			}, fullScreen :true);
		}

		public IResult PrintPriceTags()
		{
			return new DialogResult(new PrintPreviewViewModel {
				DisplayName = "Ценники",
				Document = new PriceTagDocument(Waybill, PrintableLines(), Settings.Value).Build()
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
			return Lines.Value.Where(l => l.Print).ToList();
		}

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null) {
				yield return new DialogResult(new SimpleSettings(docSettings), sizeToContent: true) {
					ShowSizeToContent = true
				};
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}

		public IResult ExportWaybill()
		{
			var columns = new [] {
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
				"Розн. цена за ед., руб",
				"Кол-во",
				"Розн. сумма, руб"
			};
			var items = PrintableLines().Select((l, i) => new object[] {
				i + 1,
				l.Product,
				String.Format("{0} {1}", l.SerialNumber, l.Certificates),
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.Quantity,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.Nds,
				l.SupplierCost,
				l.RetailMarkup,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			var book = ExcelExporter.ExportTable(columns, items, 8);
			var sheet = book.GetSheetAt(0);
			sheet.CreateRow(1).CreateCell(6).SetCellValue(String.Format("Наименование организации: Сотрудник {0}",
				Waybill.WaybillSettings.FullName));
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
			return excelExporter.Export(book);
		}

		public IResult ExportRegistry()
		{
			var columns = new [] {
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
				.SetCellValue(String.Format("полученные от {0}-по счету (накладной) №{1} от {2}",
				Waybill.SupplierName,
				Waybill.ProviderDocumentId,
				Waybill.DocumentDate.ToShortDateString()));
			return excelExporter.Export(book);
		}
	}
}