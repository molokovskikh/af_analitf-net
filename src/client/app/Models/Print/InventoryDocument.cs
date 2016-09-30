using System.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class InventoryDocument : BaseDocument
	{
		private InventoryDocLine[] _items;

		public InventoryDocument(InventoryDocLine[] items)
		{
			_items = items;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("№ п/п", 50),
				new PrintColumn("Товар", 160),
				new PrintColumn("Производитель", 160),
				new PrintColumn("Номер накладной", 50),
				new PrintColumn("Дата накладной", 50),
				new PrintColumn("Серия", 50),
				new PrintColumn("Штрихкод", 50),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена закупки с НДС", 50),
				new PrintColumn("Сумма закупки с НДС", 50),
			};

			var rows = _items.Select((o, i) => new object[] {
				o.Id,
				o.Product,
				o.Producer,
				o.WaybillNumber,
				o.DocumentDate,
				o.SerialNumber,
				o.Barcode,
				o.Quantity,
				o.SupplierCost,
				o.SupplierSum,
			});

			var table = BuildTable(rows, headers, null);
		}
	}
}

