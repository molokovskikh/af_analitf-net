using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels;

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
				new PrintColumn("розничная", 60),
				new PrintColumn("скидки", 60),
				new PrintColumn("с учетом скидки", 60),
			};

			var columnGrops = new[]
			{
				new ColumnGroup("Сумма", 5, 7),
			};

			var rows = _checks.Select((o, i) => new object[]
			{
				o.Number,
				o.Date,
				o.KKM,
				o.Department,
				o.Cancelled,
				o.RetailSum,
				o.DiscontSum,
				o.Sum,
			});

			BuildTable(rows, headers, columnGrops);
		}
	}
}
