using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.Models.Print
{
	public class BatchReport : BaseDocument
	{
		private IList<BatchLineView> lines;
		private Address address;

		public BatchReport(IList<BatchLineView> lines, Address address)
		{
			this.lines = lines;
			this.address = address;
		}

		protected override void BuildDoc()
		{
			Header($"Текущий сводный заказ от {address.Name}");
			var headers = new[] {
				new PrintColumn("Наименование", 232),
				new PrintColumn("Производитель", 168),
				new PrintColumn("Прайс-лист", 132),
				new PrintColumn("Цена", 52),
				new PrintColumn("Заказ", 45),
				new PrintColumn("Сумма", 60)
			};
			var table = BuildTableHeader(headers);
			doc.Blocks.Add(table);
			var rowGroup = table.RowGroups[0];
			foreach (var line in lines) {
				BuildRow(headers, rowGroup, new object[] {
					line.Product,
					line.Producer,
					line.OrderLine?.Order.PriceName,
					line.OrderLine?.MixedCost,
					line.Count,
					line.OrderLine?.MixedSum
				});
				var tableRow = new TableRow();
				tableRow.Cells.Add(Cell(line.Comment, 6));
				rowGroup.Rows.Add(tableRow);
			}

			if (lines.Count > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: "))) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 1
						},
						new TableCell(new Paragraph(new Run("Позиций: " + lines.Count))) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 2
						},
						new TableCell(new Paragraph(new Run("Сумма: " + lines.Where(l => l.OrderLine != null).Sum(l => l.OrderLine.MixedSum)))) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 3,
							TextAlignment = TextAlignment.Right
						}
					}
				});
			}
		}
	}
}