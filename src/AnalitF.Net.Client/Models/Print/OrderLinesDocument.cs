using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Models.Print
{
	public class OrderLinesDocument
	{
		private OrderLinesViewModel model;

		public OrderLinesDocument(OrderLinesViewModel model)
		{
			this.model = model;
		}

		public FlowDocument BuildDocument()
		{
			var doc = new FlowDocument();

			var address = ((ShellViewModel)model.Parent).CurrentAddress;

			if (model.IsCurrentSelected) {
				var text = String.Format("Текущий сводный заказ от {0}", address == null ? "" : address.Name);
				doc.Blocks.Add(new Paragraph(new Run(text)) {
					FontWeight = FontWeights.Bold,
					FontSize = 16
				});
			}
			else {
				Header(doc, String.Format("Отправленный сводный заказ {0}", address == null ? "" : address.Name));
				var text = String.Format("Период: {0} - {1}",
					model.Begin.ToShortDateString(),
					model.End.ToShortDateString());
				Header(doc, text);
			}
			var headers = new [] {
				new PrintColumnDeclaration("Наименование", 232),
				new PrintColumnDeclaration("Производитель", 168),
				new PrintColumnDeclaration("Прайс-лист", 132),
				new PrintColumnDeclaration("Цена", 52),
				new PrintColumnDeclaration("Заказ", 40),
				new PrintColumnDeclaration("Сумма", 60)
			};

			int count;
			decimal sum;
			IEnumerable<object[]> rows;

			if (model.IsCurrentSelected) {
				var lines = model.Lines;
				count = lines.Count;
				sum = lines.Sum(l => l.Sum);
				rows = model.Lines.Select(l => new object[] {
					l.ProducerSynonym,
					l.ProducerSynonym,
					l.Order.Price.Name,
					l.Cost,
					l.Count,
					l.Sum
				});
			}
			else {
				var lines = model.SentLines;
				count = lines.Count;
				sum = lines.Sum(l => l.Count * l.Cost);
				rows = model.SentLines.Select(l => new object[] {
					l.ProducerSynonym,
					l.ProducerSynonym,
					l.Order.Price.Name,
					l.Cost,
					l.Count,
					l.Count * l.Cost
				});
			}
			var table = CatalogOfferDocument.BuildTable(rows, headers, count);
			doc.Blocks.Add(table);
			if (count > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: "))) {
							BorderBrush = Brushes.Black,
							BorderThickness = new Thickness(1, 0, 0, 1),
							FontWeight = FontWeights.Bold,
							ColumnSpan = 1
						},
						new TableCell(new Paragraph(new Run("Позиций: " + count))) {
							BorderBrush = Brushes.Black,
							BorderThickness = new Thickness(1, 0, 0, 1),
							FontWeight = FontWeights.Bold,
							ColumnSpan = 2
						},
						new TableCell(new Paragraph(new Run("Сумма: " + sum))) {
							BorderBrush = Brushes.Black,
							BorderThickness = new Thickness(1, 0, 1, 1),
							FontWeight = FontWeights.Bold,
							ColumnSpan = 3
						}
					}
				});
			}

			return doc;
		}

		private static void Header(FlowDocument doc, string text)
		{
			doc.Blocks.Add(new Paragraph(new Run(text)) {
				FontWeight = FontWeights.Bold,
				FontSize = 16
			});
		}
	}
}