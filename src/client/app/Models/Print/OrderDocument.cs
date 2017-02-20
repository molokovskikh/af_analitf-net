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
		public IOrder Order;
		public IList<IOrderLine> Lines;

		public OrderDocument(IOrder order, IList<IOrderLine> lines)
		{
			Order = order;
			Lines = lines;
		}

		public OrderDocument(IOrder order)
		{
			Order = order;
			Lines = order.Lines.ToArray();
		}

		protected override void BuildDoc()
		{
			TwoColumnHeader($"Заявка № {Order.DisplayId} от {Order.CreatedOn}", Order.SafePrice?.Phone);
			Block($"Продавец: {Order.SafePrice?.SupplierFullName}\r\n"
				+ $"Покупатель: {Order.SafeAddress?.Org}\r\n{Order.SafeAddress?.Name}");
			Block($"Дата прайс-листа от {Order.SafePrice?.PriceDate}");
			Block(Order.PersonalComment);

			var headers = new[] {
				new PrintColumn("№ п/п", 45),
				new PrintColumn("Наименование", 220),
				new PrintColumn("Производитель", 165),
				new PrintColumn("Срок годн.", 73),
				new PrintColumn("Цена", 66),
				new PrintColumn("Количество", 45),
				new PrintColumn("Сумма", 80)
			};

			var count = Lines.Count;
			var sum = Lines.Sum(l => l.MixedSum);
			var rows = Lines.Select((l, i) => new object[] {
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
				BuildRow(rowGroup, headers, row);

				var line = Lines[index];
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