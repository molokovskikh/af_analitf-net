using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaybillDetails : BaseScreen
	{
		private uint id;
		private WaybillSettings waybillSettings;

		public WaybillDetails(uint id)
		{
			DisplayName = "Детализация накладной";
			this.id = id;
		}

		public Waybill Waybill { get; set; }
		public IList<WaybillLine> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Waybill = Session.Load<Waybill>(id);
			var addressId = Waybill.Address.Id;
			waybillSettings = Session.Query<WaybillSettings>().FirstOrDefault(s => s.BelongsToAddress.Id == addressId);
			if (waybillSettings == null)
				waybillSettings = new WaybillSettings();
			var markups = Session.Query<MarkupConfig>().ToList();

			Waybill.Calculate(Settings, markups, true);

			Lines = Waybill.Lines;
		}

		public IResult PrintRackingMap()
		{
			return new DialogResult(new PrintPreviewViewModel {
				DisplayName = "Стелажная карта",
				Document = new RackingMapDocument().Build(Waybill, waybillSettings, Settings)
			});
		}

		public IResult PrintPriceTags()
		{
			return null;
		}

		public IResult PrintRegistry()
		{
			return null;
		}

		public IResult PrintWaybill()
		{
			return new DialogResult(new PrintPreviewViewModel(new PrintResult("Накладная", new WaybillDocument(Waybill, waybillSettings))));
		}

		public IResult PrintInvoice()
		{
			return new DialogResult(new PrintPreviewViewModel(new PrintResult("Счет-фактура", new InvoiceDocument(Waybill))));
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
			var items = Lines.Select((l, i) => new object[] {
				i + 1,
				l.Product,
				String.Format("{0} {1}", l.SerialNumber, l.Certificates),
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.Quantity,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.NDS,
				l.SupplierCost,
				l.RetailMarkup,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			var book = excelExporter.ExportTable(columns, items, 8);
			var sheet = book.GetSheetAt(0);
			sheet.CreateRow(1).CreateCell(6).SetCellValue(String.Format("Наименование организации: Сотрудник {0}",
				waybillSettings.FullName));
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
			var items = Lines.Select((l, i) => new object[] {
				i + 1,
				l.Product,
				l.SerialNumber,
				l.Period,
				l.Producer,
				l.ProducerCost,
				l.RegistryCost,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.NDS,
				l.SupplierCost,
				l.RetailMarkup,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			var book = excelExporter.ExportTable(columns, items, 5);
			var sheet = book.GetSheetAt(0);
			sheet.CreateRow(1).CreateCell(6).SetCellValue("Реестр");
			sheet.CreateRow(2).CreateCell(3).SetCellValue("розничных цен на лекарственные средства и изделия медицинского назначения,");
			sheet.CreateRow(3).CreateCell(3).SetCellValue(String.Format("полученные от {0}-по счету (накладной) №{1} от {2}",
				Waybill.Supplier != null ? Waybill.Supplier.FullName : "",
				Waybill.ProviderDocumentId,
				Waybill.DocumentDate.ToShortDateString()));
			return excelExporter.Export(book);
		}
	}
}