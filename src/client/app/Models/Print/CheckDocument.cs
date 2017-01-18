using System;
using System.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	public class CheckDocument : BaseDocument
	{
		private Check[] _checks;

		public CheckDocument(Check[] checks)
		{
			_checks = checks;
		}

		protected override void BuildDoc()
		{
			var headers = new[]
			{
				new PrintColumn("№ чека", 45),
				new PrintColumn("Дата", 90),
				new PrintColumn("ККМ", 90),
				new PrintColumn("Отдел", 120),
				new PrintColumn("Аннулирован", 80),
				new PrintColumn("розничная", 80),
				new PrintColumn("скидки", 80),
				new PrintColumn("с учетом скидки", 80),
			};

			var columnGrops = new[]
			{
				new ColumnGroup("Сумма", 5, 7),
			};

			var rows = _checks.Select((o, i) => new object[]
			{
				o.Id,
				o.Date.ToString("dd/M/yyyy"),
				o.KKM,
				o.Address.Name,
				o.Cancelled,
				o.RetailSum,
				o.DiscountSum,
				o.Sum,
			});

			BuildTable(rows, headers, columnGrops);
		}
	}
}
