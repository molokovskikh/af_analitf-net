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
		private IList<BatchLine> lines;
		private Address address;

		public BatchReport(IList<BatchLine> lines, Address address)
		{
			this.lines = lines;
			this.address = address;
		}

		protected override void BuildDoc()
		{
			Header(string.Format("Текущий сводный заказ от {0}", address.Name));
			var headers = new[] {
				new PrintColumn("Наименование", 232),
				new PrintColumn("Производитель", 168),
				new PrintColumn("Прайс-лист", 132),
				new PrintColumn("Цена", 52),
				new PrintColumn("Заказ", 40),
				new PrintColumn("Сумма", 60)
			};
			var table = BuildTableHeader(headers);
			doc.Blocks.Add(table);
			var rowGroup = table.RowGroups[0];
			foreach (var line in lines) {
				BuildRow(headers, rowGroup, new object[] {
					line.MixedProduct,
					line.MixedProducer,
					line.PriceName,
					line.Line != null ? (decimal?)line.Line.MixedCost : null,
					line.Quantity,
					line.Line != null ? (decimal?)line.Line.MixedSum : null
				});
				var tableRow = new TableRow();
				tableRow.Cells.Add(Cell(line.Comment, 6));
				rowGroup.Rows.Add(tableRow);
			}

			if (lines.Count > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: "))) {
							BorderBrush = Brushes.Black,
							BorderThickness = new Thickness(1, 0, 0, 1),
							FontWeight = FontWeights.Bold,
							ColumnSpan = 1
						},
						new TableCell(new Paragraph(new Run("Позиций: " + lines.Count))) {
							BorderBrush = Brushes.Black,
							BorderThickness = new Thickness(1, 0, 0, 1),
							FontWeight = FontWeights.Bold,
							ColumnSpan = 2
						},
						new TableCell(new Paragraph(new Run("Сумма: " + lines.Where(l => l.Line != null).Sum(l => l.Line.MixedCost)))) {
							BorderBrush = Brushes.Black,
							BorderThickness = new Thickness(1, 0, 1, 1),
							FontWeight = FontWeights.Bold,
							ColumnSpan = 3
						}
					}
				});
			}
		}
	}
}