using System.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class ReassessmentDocument : BaseDocument
	{
		private ReassessmentLine[] _items;

		public ReassessmentDocument(ReassessmentLine[] items)
		{
			_items = items;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("Наименование товара", 160),
				new PrintColumn("Производитель", 160),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена закупки", 50),
				new PrintColumn("Наценка списания, %", 50),
				new PrintColumn("Цена списания", 50),
				new PrintColumn("Сумма списания", 50),
				new PrintColumn("Наценка приходования, %", 50),
				new PrintColumn("Цена приходования", 50),
				new PrintColumn("Сумма приходования", 50),
			};

			var rows = _items.Select((o, i) => new object[] {
				o.Product,
				o.Producer,
				o.Quantity,
				o.SupplierCostWithoutNds,
				o.SrcRetailMarkup,
				o.SrcRetailCost,
				o.SrcRetailSum,
				o.RetailMarkup,
				o.RetailCost,
				o.RetailSum,
			});

			var table = BuildTable(rows, headers, null);
		}
	}
}
