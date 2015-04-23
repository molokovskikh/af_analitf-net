﻿using System;
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
		public IOrderLine[] Lines;

		public OrderDocument(IOrder order, IOrderLine[] lines)
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
			var header = String.Format("Заявка № {0} от {1} на {2} от {3}",
				Order.DisplayId,
				Order.CreatedOn,
				Order.PriceName,
				Order.AddressName);
			TwoColumnHeader(header, Order.SafePrice.Phone);
			Block(String.Format("Дата прайс-листа от {0}", Order.SafePrice.PriceDate));
			Block(Order.PersonalComment);

			var headers = new[] {
				new PrintColumn("№ п/п", 45),
				new PrintColumn("Наименование", 220),
				new PrintColumn("Производитель", 165),
				new PrintColumn("Срок годн.", 73),
				new PrintColumn("Цена", 66),
				new PrintColumn("Заказ", 45),
				new PrintColumn("Сумма", 80)
			};

			var count = Lines.Count();
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
				BuildRow(headers, rowGroup, row);

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