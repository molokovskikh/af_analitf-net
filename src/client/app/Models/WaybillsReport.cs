using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Models.Commands;
using Common.Tools;
using Common.Tools.Calendar;
using NHibernate.Linq;
using NHibernate.Util;
using NPOI.HSSF.Record.Aggregates;
using NPOI.HSSF.UserModel;

namespace AnalitF.Net.Client.Models
{
	public class WaybillsReport : DbCommand
	{
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
			var dir = settings.MapPath("Reports");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			var filename = Path.Combine(dir, FileHelper.StringToFileName(String.Format("Росздравнадзор-{0}.xls", period)));
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
					reportRow.CreateCell(j).SetCellValue(row[j].ToString());
				}
			}
			using(var stream = File.Create(filename))
				book.Write(stream);
		}

		public Task ToTask(Config.Config config)
		{
			var task = new Task(() => {
				using(var session = Factory.OpenSession())
				using(var stateless = Factory.OpenStatelessSession()) {
					Config = config;
					Session = session;
					StatelessSession = stateless;
					Execute();
				}
			});
			return task;
		}
	}
}