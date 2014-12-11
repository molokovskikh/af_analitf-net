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
			var header = String.Format("Заявка № {0} от {1} на {2} от {3}",
				order.DisplayId,
				order.CreatedOn,
				order.PriceName,
				order.AddressName);
			TwoColumnHeader(header, order.SafePrice.Phone);
			Block(String.Format("Дата прайс-листа от {0}", order.SafePrice.PriceDate));
			Block(order.PersonalComment);

			var headers = new[] {
				new PrintColumn("№ п/п", 45),
				new PrintColumn("Наименование", 220),
				new PrintColumn("Производитель", 165),
				new PrintColumn("Срок годн.", 73),
				new PrintColumn("Цена", 66),
				new PrintColumn("Заказ", 45),
				new PrintColumn("Сумма", 80)
			};

			var lines = order.Lines.ToArray();
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

			var table = BuildTableHeader(headers);
			var rowGroup = table.RowGroups[0];
			var index = 0;
			foreach (var row in rows) {
				BuildRow(headers, rowGroup, row);

				var line = lines[index];
				if (!String.IsNullOrEmpty(line.Comment)) {
					var tableRow = new TableRow();
					rowGroup.Rows.Add(tableRow);
					var cell = new TableCell(new Paragraph(new Run(line.Comment))) {
						Style = CellStyle,
						ColumnSpan = headers.Length,
						FontStyle = FontStyles.Italic
					};
					tableRow.Cells.Add(cell);
				}

				index++;
			}

			doc.Blocks.Add(table);
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