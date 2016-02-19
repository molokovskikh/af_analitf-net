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

namespace AnalitF.Net.Client.Models.Commands
{
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
	and l.SupplierCost is not null
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
					var producerCost = row.ProducerCost == null ? 0 : Convert.ToDecimal(row.ProducerCost);
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
					writer.Write("\"" + (row.SerialNumber == null ? "-" : row.SerialNumber) + "\"");
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
		public override void Execute()
		{
			//отчет всегда готовится за предыдущий год
			var end = new DateTime(DateTime.Today.Year, 1, 1);
			var year = DateTime.Today.Year - 1;
			var begin = new DateTime(year, 1, 1);
			var rows = StatelessSession.CreateSQLQuery(@"
select
	b.Value as BarCode,
	sum(l.Quantity) / 1000 Total,
	round(avg(l.SupplierCost),2) / 1000 SupplierCost,
	round(avg(l.RetailCost),2) / 1000 RetailCost,
	round(avg(producercost),2) / 1000 ProducerCost,
	min(if(registrycost = 0, null, registrycost)) RegistryCost,
	sum(quantity) / 1000 Planned,
	(round(avg(l.RetailCost),2) - round(avg(l.SupplierCost),2)) / 1000 as Margin
from WaybillLines l
		join Waybills w on w.Id = l.WaybillId
	join BarCodes b on b.Value = l.EAN13
where b.Value = l.EAN13 and w.DocumentDate > :begin and w.DocumentDate < :end
group by b.Value;")
				.SetParameter("begin", begin)
				.SetParameter("end", end)
				.List();
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
}