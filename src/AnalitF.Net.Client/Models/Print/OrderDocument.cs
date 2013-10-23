using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Models.Print
{
	public class OrderDocument : BaseDocument
	{
		private IOrder order;

		public OrderDocument(IOrder order)
		{
			this.order = order;
		}

		protected override void BuildDoc()
		{
			var header = String.Format("Заявка № {0} от на {1} от {2}",
				order.Id,
				order.Price.Name,
				order.Address.Name);
			TwoColumnHeader(header, order.Price.Phone);
			Block(String.Format("Дата прайс-листа от {0}", order.Price.PriceDate));
			Block(order.Comment);

			var headers = new [] {
				new PrintColumn("№ п/п", 40),
				new PrintColumn("Наименование", 220),
				new PrintColumn("Производитель", 165),
				new PrintColumn("Срок годн.", 73),
				new PrintColumn("Цена", 66),
				new PrintColumn("Заказ", 40),
				new PrintColumn("Сумма", 80)
			};

			var lines = order.Lines;
			var count = lines.Count();
			var sum = lines.Sum(l => l.Sum);
			var rows = lines.Select((l, i) => new object[] {
				i + 1,
				l.ProductSynonym,
				l.ProducerSynonym,
				l.Period,
				l.Cost,
				l.Count,
				l.Sum
			});

			var table = BuildTable(rows, headers);
			if (count > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: "))) {
							BorderBrush = Brushes.Black,
							BorderThickness = new Thickness(1, 0, 0, 1),
							FontWeight = FontWeights.Bold,
							ColumnSpan = 2
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
		}
	}
}