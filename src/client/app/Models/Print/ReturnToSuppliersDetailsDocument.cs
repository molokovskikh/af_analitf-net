using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class ReturnToSuppliersDetailsDocument : BaseDocument
	{
		private ReturnToSupplierLine[] _items;

		public ReturnToSuppliersDetailsDocument(ReturnToSupplierLine[] items)
		{
			_items = items;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("№№", 50),
				new PrintColumn("Товар", 160),
				new PrintColumn("Производитель", 160),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена закупки", 50),
				new PrintColumn("Цена закупки с НДС", 50),
				new PrintColumn("Цена розничная", 50),
				new PrintColumn("Сумма закупки", 50),
				new PrintColumn("Сумма закупки с НДС", 50),
				new PrintColumn("Сумма розничная", 50),
			};

			var rows = _items.Select((o, i) => new object[] {
				o.Id,
				o.Product,
				o.Producer,
				o.Quantity,
				o.SupplierCostWithoutNds,
				o.SupplierCost,
				o.RetailCost,
				o.SupplierSumWithoutNds,
				o.SupplierSum,
				o.RetailSum,
			});

			var table = BuildTable(rows, headers, null);
		}
	}
}

