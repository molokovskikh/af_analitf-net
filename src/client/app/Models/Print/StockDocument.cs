using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class StockDocument : BaseDocument
	{
		private Stock[] _stocks;

		public StockDocument(Stock[] stocks)
		{
			_stocks = stocks;
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("Штрих-код", 90),
				new PrintColumn("Название товара", 130),
				new PrintColumn("Фирма-производитель", 130),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена закупки", 80),
				new PrintColumn("Сумма закупки с НДС", 80),
				new PrintColumn("Цена розничная", 80),
				new PrintColumn("Сумма розничная", 80),
			};

			var columnGrops = new[] {
				new ColumnGroup("Поставщик", 4, 5),
				new ColumnGroup("Аптека", 6, 7)
			};

			var rows = _stocks.Select((o, i) => new object[] {
				o.Barcode,
				o.Product,
				o.Producer,
				o.Count,
				o.Cost,
				o.SumWithNds,
				o.RetailCost,
				o.RetailSum,
			});

			var table = BuildTable(rows, headers, columnGrops);

			if (_stocks.Length > 0)
			{
				table.RowGroups[0].Rows.Add(new TableRow
				{
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 5,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(_stocks.Sum(x => x.SumWithNds).ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Center
						},
						new TableCell(new Paragraph(new Run("")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Center
						},
						new TableCell(new Paragraph(new Run(_stocks.Sum(x => x.RetailSum).ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Center
						}
					}
				});
			}

			Block($"Итого товаров на сумму\n"
				+ "По закупочной цене: " + _stocks.Sum(x => x.SumWithNds).ToString() + "\n"
				+ "По цене продажи: " + _stocks.Sum(x => x.RetailSum).ToString() + "\n\n"
				+ "Сдал:                                                                                                Принял:");
		}
	}
}
