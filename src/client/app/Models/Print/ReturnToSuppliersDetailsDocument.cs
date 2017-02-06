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
		private ReturnLine[] _items;
		private DocumentTemplate template;
		private ReturnDoc _doc;
		private WaybillSettings _waybillSettings;

		public ReturnToSuppliersDetailsDocument(ReturnLine[] items, ReturnDoc doc, WaybillSettings waybillSettings)
		{
			_items = items;
			_doc = doc;
			_waybillSettings = waybillSettings;
		}

		protected override void BuildDoc()
		{
			Block(String.Format("Содержимое документа № {0}. От {1}.", _doc.Id, _doc.Date));
			Block(String.Format("Отправитель: {0}.", _waybillSettings.Name));
			Block(String.Format("Получатель: {0}.", _doc.SupplierName));
			Block(String.Format("Комментарий: {0}.", _doc.Comment));
			var headers = new[] {
				new PrintColumn("№№", 20),
				new PrintColumn("Товар", 100),
				new PrintColumn("Производитель", 100),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена закупки", 50),
				new PrintColumn("Цена закупки с НДС", 50),
				new PrintColumn("Цена розничная", 50),
				new PrintColumn("Сумма закупки", 50),
				new PrintColumn("Сумма закупки с НДС", 50),
				new PrintColumn("Сумма розничная", 50),
				new PrintColumn("Серия", 50),
				new PrintColumn("Срок годности", 50),
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
				o.SerialNumber,
				o.Stock.Period
			});
			var table = BuildTable(rows, headers, null);
			var row = new TableRow();
				row.FontWeight = FontWeights.Bold;
				row.Cells.Add(Cell("Итого", 7));
				row.Cells.Add(Cell(_items.Sum(l => l.SupplierSumWithoutNds)));
				row.Cells.Add(Cell(_items.Sum(l => l.SupplierSum)));
				row.Cells.Add(Cell(_items.Sum(l => l.RetailSum)));
				table.RowGroups[0].Rows.Add(row);
		}

		protected override Paragraph Block(string text)
		{
			var block = base.Block(text);
			CheckTemplate(block);
			return block;
		}
		private void CheckTemplate(Paragraph block)
		{
			if (template != null) {
				doc.Blocks.Remove(block);
				Stash(block);
			}
		}
		private void Stash(FrameworkContentElement element)
		{
			template.Parts.Add(element);
			if (template.IsReady) {
				doc.Blocks.Add(template.ToBlock());
				template = null;
			}
		}
	}
}