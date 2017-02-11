using System.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class ShelfLifeDocument : BaseDocument
	{
		private Stock[] _stocks;

		public ShelfLifeDocument(Stock[] stocks)
		{
			_stocks = stocks;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("Срок годности", 110),
				new PrintColumn("Торговое наименование", 170),
				new PrintColumn("Серия", 100),
				new PrintColumn("Производитель", 170),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Номер накладной", 120),
				new PrintColumn("Поставщик", 120),
			};

			var rows = _stocks.Select((o, i) => new object[] {
				o.Period,
				o.Product,
				o.SerialNumber,
				o.Producer,
				o.Quantity,
				o.WaybillNumber,
				o.SupplierFullName,
			});

			var table = BuildTable(rows, headers, null);

		}
	}
}
