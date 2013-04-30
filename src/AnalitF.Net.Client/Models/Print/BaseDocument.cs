using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	public class BaseDocument
	{
		protected FlowDocument doc = new FlowDocument();

		protected void TwoColumnHeader(string leftHeader, string rightHeader)
		{
			var header = new Table {
				Columns = {
					new TableColumn {
						Width = new GridLength(520)
					},
					new TableColumn {
						Width = GridLength.Auto
					},
				},
				RowGroups = {
					new TableRowGroup {
						Rows = {
							new TableRow {
								Cells = {
									new TableCell(new Paragraph(new Run(leftHeader)) {
										TextAlignment = TextAlignment.Left,
										FontWeight = FontWeights.Bold,
										FontSize = 16
									}),
									new TableCell(new Paragraph(new Run(rightHeader))) {
										TextAlignment = TextAlignment.Right
									}
								}
							}
						}
					}
				}
			};
			doc.Blocks.Add(header);
		}

		public void Block(string text)
		{
			doc.Blocks.Add(new Paragraph(new Run(text)));
		}

		public void Header(string text)
		{
			doc.Blocks.Add(new Paragraph(new Run(text)) {
				FontWeight = FontWeights.Bold,
				FontSize = 16
			});
		}

		public Table BuildTable(IEnumerable<object[]> rows, PrintColumnDeclaration[] headers)
		{
			var table = BuildTableHeader(headers);
			var tableRowGroup = table.RowGroups[0];

			var j = 0;
			foreach (var data in rows) {
				BuildRow(headers, tableRowGroup, data, j);
				j++;
			}
			var row = tableRowGroup.Rows.Last();
			foreach (var cell in row.Cells) {
				cell.BorderThickness = new Thickness(1, 1, 1, 1);
			}

			doc.Blocks.Add(table);
			return table;
		}

		protected static void BuildRow(PrintColumnDeclaration[] headers, TableRowGroup tableRowGroup, object[] data, int index)
		{
			var row = new TableRow();
			tableRowGroup.Rows.Add(row);

			for (var i = 0; i < data.Length; i++) {
				var text = "";
				var value = data[i];
				if (value != null)
					text = value.ToString();

				var cell = new TableCell(new Paragraph(new Run(text)));
				if (Util.IsDigitValue(value)) {
					cell.TextAlignment = TextAlignment.Right;
				}
				cell.BorderBrush = Brushes.Black;
				var thickness = new Thickness(1, 1, 0, 0);
				if (i == data.Length - 1)
					thickness.Right = 1;
				cell.BorderThickness = thickness;
				row.Cells.Add(cell);
			}
		}

		protected static Table BuildTableHeader(PrintColumnDeclaration[] headers)
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
			return table;
		}
	}
}