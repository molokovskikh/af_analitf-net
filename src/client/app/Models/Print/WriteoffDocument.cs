using System.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class WriteoffDocument : BaseDocument
	{
		private WriteoffLine[] _items;

		public WriteoffDocument(WriteoffLine[] items)
		{
			_items = items;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("№№", 50),
				new PrintColumn("Товар", 210),
				new PrintColumn("Производитель", 210),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена закупки", 50),
				new PrintColumn("Цена розничная", 50),
				new PrintColumn("Сумма закупки", 50),
				new PrintColumn("Сумма розничная", 50),
			};

			var rows = _items.Select((o, i) => new object[] {
				o.Id,
				o.Product,
				o.Producer,
				o.Quantity,
				o.SupplierCostWithoutNds,
				o.RetailCost,
				o.SupplierSumWithoutNds,
				o.RetailSum,
			});

			var table = BuildTable(rows, headers, null);
		}
	}
}


