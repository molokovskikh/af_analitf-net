using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Common.Tools.Calendar;
using Dapper;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace AnalitF.Net.Client.Models.Commands
{
	public class GoodsMovementReportSettings
	{
		public DateTime Begin { get; set; }
		public DateTime End { get; set; }

		public uint[] AddressIds { get; set; }
		public string AddressNames { get; set; }

		public uint[] CatalogIds { get; set; }
		public string CatalogNames { get; set; }

		public uint[] SupplierIds { get; set; }
		public string SupplierNames { get; set; }

		public uint[] ProducerIds { get; set; }
		public string ProducerNames { get; set; }

		public bool FilterByWriteTime { get; set; }

		public GoodsMovementReportSettings()
		{
			AddressIds = CatalogIds = SupplierIds = ProducerIds = new uint[] { };
			AddressNames = CatalogNames = SupplierNames = ProducerNames = "все";
		}
	}

	public class GoodsMovementReport : DbCommand<string>
	{
		private GoodsMovementReportSettings _settings;

		public class GoodsMovementRow
		{
			public string Name { get; set; }
			public DateTime WriteTime { get; set; }
			public string ProviderDocumentId { get; set; }
			public string UserSupplierName { get; set; }
			public decimal Quantity { get; set; }
			public decimal SupplierSum { get; set; }
			public decimal RetailSum { get; set; }
		}

		public GoodsMovementReport(GoodsMovementReportSettings settings)
		{
			_settings = settings;
		}

		public override void Execute()
		{
			var settings = Session.Query<Settings>().First();
			var dir = settings.InitAndMap("Reports");
			Result = Path.Combine(dir, FileHelper.StringToFileName($"Движение товара по накладным-{_settings.Begin:d}-{_settings.End:d}.xls"));

			var field = "WriteTime";
			if (_settings.FilterByWriteTime)
				field = "DocumentDate";
			var sql = $@"
select CONCAT_WS(' ', l.Product, l.SerialNumber, d.InnR, l.Certificates) as Name,
w.WriteTime, w.ProviderDocumentId, w.UserSupplierName,
SUM(l.Quantity) as Quantity, SUM(l.SupplierCost*l.Quantity) as SupplierSum, SUM(l.RetailCost*l.Quantity) as RetailSum
from WaybillLines l
join Waybills w on w.Id = l.WaybillId
left join Drugs d on d.EAN = l.EAN13
where DocType = 1
	and Status = 1
	and w.{field} > ?
	and w.{field} < ?
";
			if (_settings.AddressIds.Any())
				sql += $" and w.AddressId in ({_settings.AddressIds.Implode()})";
			if (_settings.CatalogIds.Any())
				sql += $" and l.CatalogId in ({_settings.CatalogIds.Implode()})";
			if (_settings.SupplierIds.Any())
				sql += $" and w.SupplierId in ({_settings.SupplierIds.Implode()})";
			if (_settings.ProducerIds.Any())
				sql += $" and l.ProducerId in ({_settings.ProducerIds.Implode()})";
			sql += @" group by Name, w.WriteTime, w.ProviderDocumentId, w.UserSupplierName
								order by Name asc, w.WriteTime asc;";

			var book = new HSSFWorkbook();

			var headerStyle = book.CreateCellStyle();
			headerStyle.Alignment = HorizontalAlignment.Center;

			var groupStyle = book.CreateCellStyle();
			groupStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
			groupStyle.FillPattern = FillPattern.SolidForeground;
			groupStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

			var boldFont = book.CreateFont();
			boldFont.Boldweight = (short)FontBoldWeight.Bold;
			var totalStyle = book.CreateCellStyle();
			totalStyle.Alignment = HorizontalAlignment.Right;
			totalStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
			totalStyle.SetFont(boldFont);

			var numericStyle = book.CreateCellStyle();
			numericStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

			var sheet = book.CreateSheet("Движение товара по накладным");
			var rowIndex = 0;

			var headers = new[]
			{
				$"Движение товаров за период {_settings.Begin.ToShortDateString()} — {_settings.End.ToShortDateString()}",
				"По фирме: все",
				$"По отделу: {_settings.AddressNames}",
				$"По товару: {_settings.CatalogNames}",
				$"По поставщику: {_settings.SupplierNames}",
				$"По производителю: {_settings.ProducerNames}",
				"",
			};
			foreach (var header in headers)
				ExcelExporter.WriteRow(sheet, new object[] { header }, rowIndex++);

			var columns = new object[] {
				"Товар / Документ движения / Серия / МНН / Номер сертификата",
				"",
				"Получатель / Поставщик",
				"Приход, шт.",
				"Сумма опт, руб.",
				"Сумма розница, руб."
			};
			ExcelExporter.WriteRow(sheet, columns, rowIndex).Cells.ForEach(x => x.CellStyle = headerStyle);
			MergeCells(sheet, rowIndex++);

			var items = StatelessSession.Connection.Query<GoodsMovementRow>(sql, new { begin = _settings.Begin, end = _settings.End.AddDays(1) });
			var groups = items.GroupBy(x => x.Name)
				.Select(x => new object[]{ x.Key,
					"",
					"",
					x.Sum(y => y.Quantity),
					x.Sum(y => y.SupplierSum),
					x.Sum(y => y.RetailSum)
				}).ToList();

			foreach (var group in groups) {
				ExcelExporter.WriteRow(sheet, group, rowIndex).Cells.ForEach(x => x.CellStyle = groupStyle);
				MergeCells(sheet, rowIndex++);
				var rows = items.Where(x => x.Name == (string)group[0]).Select((o, i) => new object[] {
					o.WriteTime.ToString("dd.MM.yyyy HH:mm"),
					$"Приходная накладная {o.ProviderDocumentId}",
					o.UserSupplierName,
					o.Quantity,
					o.SupplierSum,
					o.RetailSum,
				});
				foreach (var row in rows)
					ExcelExporter.WriteRow(sheet, row, rowIndex++).Cells.ForEach(x => x.CellStyle = numericStyle);
			}

			if (items.Any()) {
				WriteStatRow(sheet, rowIndex, items, "ВСЕГО").Cells.ForEach(x => x.CellStyle = totalStyle);
				MergeCells(sheet, rowIndex++);
			}

			for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
				sheet.AutoSizeColumn(columnIndex, true);

			using (var stream = File.Create(Result))
				book.Write(stream);
		}

		private static void MergeCells(ISheet sheet, int rowIndex)
		{
			var cra = new CellRangeAddress(rowIndex, rowIndex, 0, 1);
			sheet.AddMergedRegion(cra);
		}

		private static IRow WriteStatRow(ISheet sheet, int rowIndex, IEnumerable<GoodsMovementRow> items, string label)
		{
			var row = sheet.CreateRow(rowIndex);
			ExcelExporter.SetCellValue(row, 0, label);
			ExcelExporter.SetCellValue(row, 3, items.Sum(x => x.Quantity));
			ExcelExporter.SetCellValue(row, 4, items.Sum(x => x.SupplierSum));
			ExcelExporter.SetCellValue(row, 5, items.Sum(x => x.RetailSum));
			return row;
		}
	}

	public class ConsumptionReport : DbCommand<string>
	{
		private Waybill _waybill;

		public class ConsumptionDocumentRow
		{
			public string Name { get; set; }
			public decimal Quantity { get; set; }
			public string DocType { get; set; }
			public uint DocumentId { get; set; }
			public DateTime Date { get; set; }
			public decimal Quantity2 { get; set; }
		}

		public ConsumptionReport(Waybill waybill)
		{
			_waybill = waybill;
		}

		public override void Execute()
		{
			var settings = Session.Query<Settings>().First();
			var dir = settings.InitAndMap("Reports");
			Result = Path.Combine(dir, FileHelper.StringToFileName($"Расход по документу {_waybill.ProviderDocumentId}.xls"));

			var book = new HSSFWorkbook();

			var titleFont = book.CreateFont();
			titleFont.Boldweight = (short)FontBoldWeight.Bold;
			titleFont.FontHeightInPoints = 12;
			var titleStyle = book.CreateCellStyle();
			titleStyle.Alignment = HorizontalAlignment.Center;
			titleStyle.VerticalAlignment = VerticalAlignment.Center;
			titleStyle.SetFont(titleFont);

			var headerFont = book.CreateFont();
			headerFont.Boldweight = (short)FontBoldWeight.Bold;
			var headerStyle = book.CreateCellStyle();
			headerStyle.Alignment = HorizontalAlignment.Center;
			headerStyle.VerticalAlignment = VerticalAlignment.Center;
			headerStyle.WrapText = true;
			headerStyle.SetFont(headerFont);

			var numericStyle = book.CreateCellStyle();
			numericStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

			var wrapStyle = book.CreateCellStyle();
			wrapStyle.WrapText = true;

			var sheet = book.CreateSheet($"Расход по документу {_waybill.ProviderDocumentId}");
			var rowIndex = 0;

			var titles = new[]
			{
				$"Расход по документу {_waybill.ProviderDocumentId}",
				"",
			};
			sheet.AddMergedRegion(new CellRangeAddress(rowIndex, rowIndex, 0, 4));
			foreach (var header in titles)
				ExcelExporter.WriteRow(sheet, new object[] { header }, rowIndex++).Cells.ForEach(x => x.CellStyle = titleStyle);

			sheet.AddMergedRegion(new CellRangeAddress(rowIndex, rowIndex + 1, 0, 0));
			sheet.AddMergedRegion(new CellRangeAddress(rowIndex, rowIndex + 1, 1, 1));
			sheet.AddMergedRegion(new CellRangeAddress(rowIndex, rowIndex, 2, 4));
			var columns = new object[] {
				"Наименование\nПроизводитель",
				"Кол-во",
				"Расход"
			};
			ExcelExporter.WriteRow(sheet, columns, rowIndex++).Cells.ForEach(x => x.CellStyle = headerStyle);

			var columns2 = new object[] {
				null,
				null,
				"Документ",
				"Дата",
				"Кол-во"
			};
			ExcelExporter.WriteRow(sheet, columns2, rowIndex++).Cells.ForEach(x => x.CellStyle = headerStyle);

			var sql = $@"
drop temporary table if exists consumption_report;
create temporary table consumption_report engine=memory
select wl.Product, wl.Producer, wl.SerialNumber, wl.Quantity,
'Списание' as DocType, d.Id as DocumentId, d.Date, l.Quantity as Quantity2
from writeofflines l
join writeoffdocs d on d.Id = l.WriteoffDocId
join waybilllines wl on wl.Id = l.WaybillLineId
where d.`Status` = 1 and wl.WaybillId = ?
union all
select wl.Product, wl.Producer, wl.SerialNumber, wl.Quantity,
'Возврат поставщику' as DocType, d.Id as DocumentId, d.Date, l.Quantity as Quantity2
from ReturnLines l
	join ReturnDocs d on d.Id = l.ReturnDocId
join waybilllines wl on wl.Id = l.WaybillLineId
where d.`Status` = 1 and wl.WaybillId = ?
union all
select wl.Product, wl.Producer, wl.SerialNumber, wl.Quantity,
'Внутреннее перемещение' as DocType, d.Id as DocumentId, d.Date, l.Quantity as Quantity2
from displacementlines l
join displacementdocs d on d.Id = l.DisplacementDocId
join waybilllines wl on wl.Id = l.WaybillLineId
where (d.`Status` = 1 or d.`Status` = 2) and wl.WaybillId = ?
union all
select wl.Product, wl.Producer, wl.SerialNumber, wl.Quantity,
'Чек' as DocType, d.Id as DocumentId, d.Date, l.Quantity as Quantity2
from checklines l
join checks d on d.Id = l.CheckId
join waybilllines wl on wl.Id = l.WaybillLineId
where d.`Status` = 0 and d.CheckType = 0 and wl.WaybillId = ?
;";

			StatelessSession.Connection.Execute(sql, new { a = _waybill.Id, b = _waybill.Id, c = _waybill.Id, d = _waybill.Id });

			sql = $@"
select CONCAT(Product, '\n', Producer) as Name, SUM(Quantity) as Quantity,
DocType, DocumentId, Date, SUM(Quantity2) as Quantity2
from consumption_report
group by Product, Producer, SerialNumber, DocType, DocumentId, Date
order by Name asc, Date desc
;";
			var items = StatelessSession.Connection.Query<ConsumptionDocumentRow>(sql);
			var prevName = "";
			foreach (var item in items)
			{
				if (item.Name == prevName)
					item.Name = "";
				else
					prevName = item.Name;
			}

			var rows = items.Select(x => new object[]{
							x.Name,
							x.Quantity,
							$"{x.DocType} №{x.DocumentId}",
							x.Date.ToString("dd.MM.yyyy"),
							x.Quantity2
						}).ToList();

			foreach (var row in rows)
			{
				var cells = ExcelExporter.WriteRow(sheet, row, rowIndex++).Cells;
				cells[0].CellStyle = wrapStyle;
				cells[1].CellStyle = numericStyle;
				cells[4].CellStyle = numericStyle;
			}

			for (int columnIndex = 0; columnIndex < columns2.Length; columnIndex++)
				sheet.AutoSizeColumn(columnIndex, true);
			// увеличил первую колонку относительно значения по автосайзу, т.к. при WrapText = true текст переносится не только по \n
			sheet.SetColumnWidth(0, sheet.GetColumnWidth(0) * 2);

			using (var stream = File.Create(Result))
				book.Write(stream);
		}
	}

	public class VitallyImportantReport : DbCommand<string>
	{
		public DateTime Begin;
		public DateTime End;
		public uint[] AddressIds;
		public bool FilterByWriteTime;

		public override void Execute()
		{
			var settings = Session.Query<Settings>().First();
			var dir = settings.InitAndMap("Reports");
			Result = Path.Combine(dir, FileHelper.StringToFileName($"Росздравнадзор-ЖНВЛП-{Begin:d}-{End:d}.csv"));

			var field = "WriteTime";
			if (FilterByWriteTime)
				field = "DocumentDate";
			var sql = $@"
drop temporary table if exists uniq_document_lines;
create temporary table uniq_document_lines engine=memory
select max(l.Id) as Id
from WaybillLines l
	join Waybills w on w.Id = l.WaybillId
		join Suppliers s on s.Id = w.SupplierId
	join Drugs d on d.EAN = l.EAN13
where w.{field} > ?
	and w.{field} < ?
	and w.AddressId in ({AddressIds.Implode()})
	and l.Quantity is not null
	and l.ProducerCost is not null
	and l.producerCost > 0
	and l.SupplierCost is not null
	and l.RetailCost is not null
	and l.RetailCost > 0
	and s.VendorID is not null
group by l.EAN13
;";

			StatelessSession.Connection.Execute(sql, new { Begin, end = End.AddDays(1) });
			sql = @"
select l.Quantity, l.ProducerCost, l.SerialNumber, l.NDS, l.SupplierCost, s.VendorId, d.DrugId, d.MaxMnfPrice, l.RetailCost
from WaybillLines l
	join Waybills w on w.Id = l.WaybillId
		join Suppliers s on s.Id = w.SupplierId
	join uniq_document_lines u on u.Id = l.Id
	join Drugs d on d.EAN = l.EAN13
;";

			using (var stream = File.Create(Result))
			using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1251))) {
				writer.WriteLine("DrugID;Segment;Year;Month;Series;TotDrugQn;MnfPrice;PrcPrice;RtlPrice;Funds;VendorID;Remark;SrcOrg");
				foreach (var row in StatelessSession.Connection.Query(sql)) {
					var producerCost = Convert.ToDecimal(row.ProducerCost);
					var supplierCost = Convert.ToDecimal(row.SupplierCost);
					var nds = row.NDS == null ? 10 : Convert.ToDecimal(row.NDS);

					var producerCostForReport = Math.Round(producerCost * (1 + nds / 100), 2);

					decimal maxProducerCost;
					if (decimal.TryParse(row.MaxMnfPrice.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out maxProducerCost) && maxProducerCost > 0) {
						if (producerCost > maxProducerCost)
							continue;

						if (producerCost / producerCostForReport > 10)
							continue;
					}

					if ((producerCostForReport - supplierCost) / producerCostForReport > 0.25m)
						continue;

					writer.Write(row.DrugId);
					writer.Write(";");
					writer.Write(1);
					writer.Write(";");
					writer.Write(DateTime.Now.Year);
					writer.Write(";");
					writer.Write(DateTime.Now.Month);
					writer.Write(";");
					writer.Write("\"" + (row.SerialNumber ?? "-") + "\"");
					writer.Write(";");
					writer.Write(Convert.ToDecimal(row.Quantity).ToString("0.00", CultureInfo.InvariantCulture));
					writer.Write(";");
					writer.Write(producerCostForReport.ToString("0.00", CultureInfo.InvariantCulture));
					writer.Write(";");
					writer.Write(supplierCost.ToString("0.00", CultureInfo.InvariantCulture));
					writer.Write(";");
					writer.Write(row.RetailCost.ToString("0.00", CultureInfo.InvariantCulture));
					writer.Write(";");
					writer.Write(0.ToString("0.00", CultureInfo.InvariantCulture));
					writer.Write(";");
					writer.Write((string)row.VendorId);
					writer.WriteLine();
				}
			}

			sql = "drop temporary table if exists uniq_document_lines;";
			StatelessSession.Connection.Execute(sql);
		}
	}

	public class WaybillsReport : DbCommand<string>
	{
		public WaybillsReport(Config.Config config)
		{
			Config = config;
		}

		public override void Execute()
		{
			var end = DateTime.Today.FirstDayOfWeek();
			var begin = end.AddDays(-7);
			var period = end.ToShortDateString();
			var rows = StatelessSession.CreateSQLQuery(@"
select r.DrugID, r.InnR, r.TradeNmR, r.DrugFmNmRS, r.Pack, r.DosageR, r.ClNm, r.Segment,
	:period as RptPeriod,
	l.SupplierCost as PrcPrice,
	cast(round(min(l.RetailCost), 2) as char) as RtlPrice
from WaybillLines l
	join RegulatorRegistry r on r.ProductId = l.ProductId and r.ProducerId = l.ProducerId
	join Waybills w on w.Id = l.WaybillId
where w.DocumentDate >= :begin and w.DocumentDate < :end and l.RetailCost is not null
group by r.DrugID")
				.SetParameter("begin", begin)
				.SetParameter("end", end)
				.SetParameter("period", period)
				.List();
			var settings = Session.Query<Settings>().First();
			var dir = settings.InitAndMap("Reports");
			Result = Path.Combine(dir, FileHelper.StringToFileName($"Росздравнадзор-{period}.xls"));
			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("Отчет");
			var reportRow = sheet.CreateRow(0);
			reportRow.CreateCell(0).SetCellValue("DrugID");
			reportRow.CreateCell(1).SetCellValue("InnR");
			reportRow.CreateCell(2).SetCellValue("TradeNmR");
			reportRow.CreateCell(3).SetCellValue("DrugFmNmRS");
			reportRow.CreateCell(4).SetCellValue("Pack");
			reportRow.CreateCell(5).SetCellValue("DosageR");
			reportRow.CreateCell(6).SetCellValue("ClNm");
			reportRow.CreateCell(7).SetCellValue("Segment");
			reportRow.CreateCell(8).SetCellValue("RptPeriod");
			reportRow.CreateCell(9).SetCellValue("PrcPrice");
			reportRow.CreateCell(10).SetCellValue("RtlPrice");
			for(var i = 0; i < rows.Count; i++) {
				reportRow = sheet.CreateRow(i + 1);
				var row = ((object[])rows[i]);
				for (var j = 0; j < row.Length; j++) {
					reportRow.CreateCell(j).SetCellValue(row[j]?.ToString());
				}
			}
			using(var stream = File.Create(Result))
				book.Write(stream);
		}
	}

	public class WaybillMarkupReport : DbCommand<string>
	{
		public bool withNds = false;

		public override void Execute()
		{
			//отчет всегда готовится за предыдущий год
			var end = new DateTime(DateTime.Today.Year, 1, 1);
			var year = DateTime.Today.Year - 1;
			var begin = new DateTime(year, 1, 1);
			var subrows = "sum(SupplierCost) / 1000 {0} SupplierCost, sum(RetailCost) / 1000 {0} RetailCost, sum(ProducerCost) / 1000 {0} ProducerCost";
			if (withNds)
			{
				subrows = string.Format(subrows, "* 1.1");
			}
			else
			{
				subrows = string.Format(subrows, "");
			}

			var query = StatelessSession.CreateSQLQuery($@"
select
BarCode,
sum(Total) Total,
{subrows},
min(if(registrycost = 0, null, registrycost)) RegistryCost,
sum(Planned) Planned,
(round(avg(RetailCostM),2) - round(avg(SupplierCostM),2)) / 1000 as Margin
from
(select
	b.Value as BarCode,
	l.Quantity / 1000 Total,
	round(l.SupplierCost * l.Quantity, 2) SupplierCost,
	round(l.RetailCost * l.Quantity, 2) RetailCost,
	round(producercost  * l.Quantity,2) ProducerCost,
	RegistryCost,
	quantity / 1000 Planned,
	RetailCost RetailCostM,
  SupplierCost SupplierCostM
from WaybillLines l
		join Waybills w on w.Id = l.WaybillId
	join BarCodes b on b.Value = l.EAN13
where b.Value = l.EAN13 and w.DocumentDate > :begin and w.DocumentDate < :end
group by l.id) as sub
group by BarCode;");
			query.SetParameter("begin", begin);
			query.SetParameter("end", end);
			var rows =	query.List();
			var settings = Session.Query<Settings>().First();
			var dir = settings.InitAndMap("Reports");
			Result = Path.Combine(dir, FileHelper.StringToFileName($"Надб-ЖНВЛС-{year}.xls"));
			var book = new HSSFWorkbook();
			var sheet = book.CreateSheet("ЛС");
			var reportRow = sheet.CreateRow(0);
			reportRow.CreateCell(0).SetCellValue("Штрихкод");
			reportRow.CreateCell(1).SetCellValue("Количество");
			reportRow.CreateCell(2).SetCellValue("СтоимостьПриобр");
			reportRow.CreateCell(3).SetCellValue("СтоимостьРеализ");
			reportRow.CreateCell(4).SetCellValue("СтоимостьВЦенахПроизв");
			reportRow.CreateCell(5).SetCellValue("ПредельнаяЦенаПроизв");
			reportRow.CreateCell(6).SetCellValue("КоличествоПлан");
			reportRow.CreateCell(7).SetCellValue("ВаловаяПрибыльПлан");

			var converter = new SlashNumber();
			for (var i = 0; i < rows.Count; i++) {
				reportRow = sheet.CreateRow(i + 1);
				var row = ((object[])rows[i]);
				for (var j = 0; j < row.Length; j++) {
					if (j == 0) {
						reportRow.CreateCell(j).SetCellValue(row[j]?.ToString());
						continue;
					}
					if(j > 1 && j < 5)
					{
						reportRow.CreateCell(j).SetCellValue(converter.Convert(Convert.ToDouble(row[j]), 5));
						continue;
					}
					reportRow.CreateCell(j).SetCellValue(Convert.ToDouble(row[j]));
				}
			}
			using(var stream = File.Create(Result))
				book.Write(stream);
		}
	}
}