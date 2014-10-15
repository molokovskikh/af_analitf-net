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
		public IOrder order;

		public OrderDocument(IOrder order)
		{
			this.order = order;
		}

		protected override void BuildDoc()
		{
			var header = String.Format("Заявка № {0} от на {1} от {2}",
				order.DisplayId,
				order.PriceName,
				order.AddressName);
			TwoColumnHeader(header, order.SafePrice.Phone);
			Block(String.Format("Дата прайс-листа от {0}", order.SafePrice.PriceDate));
			Block(order.Comment);

			var headers = new[] {
				new PrintColumn("№ п/п", 45),
				new PrintColumn("Наименование", 220),
				new PrintColumn("Производитель", 165),
				new PrintColumn("Срок годн.", 73),
				new PrintColumn("Цена", 66),
				new PrintColumn("Заказ", 45),
				new PrintColumn("Сумма", 80)
			};

			var lines = order.Lines;
			var count = lines.Count();
			var sum = lines.Sum(l => l.MixedSum);
			var rows = lines.Select((l, i) => new object[] {
				i + 1,
				l.ProductSynonym,
				l.ProducerSynonym,
				l.Period,
				l.ResultCost,
				l.Count,
				l.MixedSum
			});

			var table = BuildTable(rows, headers);
			if (count > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: "))) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 2,
						},
						new TableCell(new Paragraph(new Run("Позиций: " + count))) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 2,
						},
						new TableCell(new Paragraph(new Run("Сумма: " + sum))) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 3,
							TextAlignment = TextAlignment.Right,
						}
					}
				});
			}
		}
	}
}