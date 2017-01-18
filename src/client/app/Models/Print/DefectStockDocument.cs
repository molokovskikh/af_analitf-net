using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class DefectStockDocument : BaseDocument
	{
		private Stock[] _stocks;

		public DefectStockDocument(Stock[] stocks)
		{
			_stocks = stocks;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("Штрих-код", 110),
				new PrintColumn("Товар", 170),
				new PrintColumn("Производитель", 170),
				new PrintColumn("Серия", 100),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Брак", 120),
			};

			var rows = _stocks.Select((o, i) => new object[] {
				o.Barcode,
				o.Product,
				o.Producer,
				o.SerialNumber,
				o.Quantity,
				o.RejectStatusName,
			});

			var table = BuildTable(rows, headers, null);

		}
	}
}
