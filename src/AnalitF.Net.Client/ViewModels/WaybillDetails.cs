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
				DisplayName = "��������� �����",
				Document = new RackingMapDocument().Build(Waybill, waybillSettings, Settings)
			});
		}

		public IResult ExportWaybill()
		{
			var columns = new [] {
				"� ��",
				"������������ � ������� �������������� ������",
				"����� ������ ����������",
				"���� ��������",
				"�������������",
				"���� ��� ���, ���",
				"����������.�����.",
				"���. ����. %",
				"������. ���� ���-�� ��� ���, ���",
				"��� ���-��, ���",
				"������. ���� ���-�� � ���, ���",
				"����. ����. ����. %",
				"����. ���� �� ��., ���",
				"���-��",
				"����. �����, ���"
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
			sheet.CreateRow(1).CreateCell(6).SetCellValue(String.Format("������������ �����������: ��������� {0}",
				waybillSettings.FullName));
			var row = sheet.CreateRow(2);
			row.CreateCell(3).SetCellValue("�����:");
			row.CreateCell(4).SetCellValue("_______________________________________");

			row = sheet.CreateRow(3);
			row.CreateCell(0).SetCellValue("���������� �");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("��������� �");
			row.CreateCell(6).SetCellValue("_______________________");

			row = sheet.CreateRow(4);
			row.CreateCell(1).SetCellValue("�� \"___\"_________________20___�");
			row.CreateCell(6).SetCellValue("�� \"___\"_________________20___�");

			row = sheet.CreateRow(5);
			row.CreateCell(0).SetCellValue("����: �������� �����");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("����� ����");
			row.CreateCell(6).SetCellValue("_______________________");

			row = sheet.CreateRow(6);
			row.CreateCell(0).SetCellValue("��������� �������");
			row.CreateCell(1).SetCellValue("_______________________");
			row.CreateCell(5).SetCellValue("������������ �_____");
			row.CreateCell(6).SetCellValue("�� \"___\"_________________20___�");
			return excelExporter.Export(book);
		}

		public IResult ExportRegistry()
		{
			var columns = new [] {
				"� ��",
				"������������",
				"����� ������",
				"���� ��������",
				"�������������",
				"���� ��� ���, ���",
				"���� ��, ���",
				"���. ����. %",
				"������. ���� ���-�� ��� ���, ���",
				"��� ���-��, ���",
				"������. ���� ���-�� � ���, ���",
				"����. ����. ����. %",
				"����. ���� �� ��., ���",
				"���-��",
				"����. �����, ���"
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
			sheet.CreateRow(1).CreateCell(6).SetCellValue("������");
			sheet.CreateRow(2).CreateCell(3).SetCellValue("��������� ��� �� ������������� �������� � ������� ������������ ����������,");
			sheet.CreateRow(3).CreateCell(3).SetCellValue(String.Format("���������� �� {0}-�� ����� (���������) �{1} �� {2}",
				Waybill.Supplier != null ? Waybill.Supplier.FullName : "",
				Waybill.ProviderDocumentId,
				Waybill.DocumentDate.ToShortDateString()));
			return excelExporter.Export(book);
		}
	}
}