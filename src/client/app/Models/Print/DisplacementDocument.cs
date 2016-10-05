using System.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class DisplacementDocument : BaseDocument
	{
		private DisplacementLine[] _items;

		public DisplacementDocument(DisplacementLine[] items)
		{
			_items = items;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("№№", 50),
				new PrintColumn("Товар", 185),
				new PrintColumn("Производитель", 185),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена закупки с НДС", 50),
				new PrintColumn("Сумма закупки с НДС", 50),
				new PrintColumn("Серия", 50),
				new PrintColumn("Срок", 50),
				new PrintColumn("Штрихкод", 50),
			};

			var rows = _items.Select((o, i) => new object[] {
				o.Id,
				o.Product,
				o.Producer,
				o.Quantity,
				o.SupplierCost,
				o.SupplierSum,
				o.SerialNumber,
				o.Period,
				o.Barcode,
			});

			var table = BuildTable(rows, headers, null);
		}
	}
}
