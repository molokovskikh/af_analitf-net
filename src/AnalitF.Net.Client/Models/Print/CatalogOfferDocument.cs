using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	public class PrintColumnDeclaration
	{
		public PrintColumnDeclaration(string name, int width)
		{
			Name = name;
			Width = width;
		}

		public string Name;
		public int Width;
	}

	public class CatalogOfferDocument
	{
		private List<Offer> offers;
		private string reportHeader;

		public CatalogOfferDocument(List<Offer> offers, string header)
		{
			this.offers = offers;
			reportHeader = header;
		}

		public FlowDocument BuildDocument()
		{
			var rows = offers.Select((o, i) => new object[] {
				o.ProductSynonym,
				o.ProducerSynonym,
				o.Price.Name,
				o.Period,
				o.Price.PriceDate,
				o.Diff,
				o.Cost
			});

			return BuildDocument(rows);
		}

		private FlowDocument BuildDocument(IEnumerable<object[]> rows)
		{
			var totalRows = offers.Count();
			var doc = new FlowDocument();

			doc.Blocks.Add(new Paragraph());
			doc.Blocks.Add(new Paragraph(new Run(reportHeader)) {
				FontWeight = FontWeights.Bold,
				FontSize = 16
			});

			var headers = new [] {
				new PrintColumnDeclaration("Наименование", 216),
				new PrintColumnDeclaration("Производитель", 136),
				new PrintColumnDeclaration("Прайс-лист", 112),
				new PrintColumnDeclaration("Срок год.", 85),
				new PrintColumnDeclaration("Дата пр.", 85),
				new PrintColumnDeclaration("Разн.", 48),
				new PrintColumnDeclaration("Цена", 55)
			};

			var table = BuildTable(rows, headers, totalRows);
			doc.Blocks.Add(table);
			doc.Blocks.Add(new Paragraph(new Run(String.Format("Общее количество предложений: {0}", totalRows))));
			return doc;
		}

		public static Table BuildTable(IEnumerable<object[]> rows, PrintColumnDeclaration[] headers, int totalRows)
		{
			var table = new Table {
				CellSpacing = 0,
				FontSize = 10,
			};

			foreach (var header in headers) {
				table.Columns.Add(new TableColumn {
					Width = new GridLength(header.Width)
				});
			}

			var tableRowGroup = new TableRowGroup();
			table.RowGroups.Add(tableRowGroup);

			var headerRow = new TableRow();
			for (var i = 0; i < headers.Length; i++) {
				var header = headers[i];
				var tableCell = new TableCell(new Paragraph(new Run(header.Name))) {
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 0, 0),
					FontWeight = FontWeights.Bold,
					LineStackingStrategy = LineStackingStrategy.MaxHeight
				};
				headerRow.Cells.Add(tableCell);
				if (i == headers.Length - 1)
					tableCell.BorderThickness = new Thickness(1, 1, 1, 0);
			}
			tableRowGroup.Rows.Add(headerRow);

			var j = 0;
			foreach (var data in rows) {
				var row = new TableRow();
				tableRowGroup.Rows.Add(row);

				for (var i = 0; i < data.Length; i++) {
					string text = "";
					if (data[i] != null)
						text = data[i].ToString();

					var cell = new TableCell(new Paragraph(new Run(text)));
					cell.BorderBrush = Brushes.Black;
					var thickness = new Thickness(1, 1, 0, 0);
					if (i == headers.Length - 1)
						thickness.Right = 1;
					if (j == totalRows - 1)
						thickness.Bottom = 1;
					cell.BorderThickness = thickness;
					row.Cells.Add(cell);
				}
				j++;
			}

			return table;
		}
	}
}