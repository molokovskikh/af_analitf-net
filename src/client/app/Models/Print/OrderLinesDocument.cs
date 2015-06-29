﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;

namespace AnalitF.Net.Client.Models.Print
{
	public class OrderLinesDocument : BaseDocument
	{
		private OrderLinesViewModel model;

		public OrderLinesDocument(OrderLinesViewModel model)
		{
			this.model = model;
		}

		protected override void BuildDoc()
		{
			var address = ((ShellViewModel)model.Parent).CurrentAddress;
			if (model.IsCurrentSelected) {
				var text = String.Format("Текущий сводный заказ от {0}", address == null ? "" : address.Name);
				doc.Blocks.Add(new Paragraph(new Run(text)) {
					FontWeight = FontWeights.Bold,
					FontSize = 16
				});
			}
			else {
				Header(String.Format("Отправленный сводный заказ {0}", address == null ? "" : address.Name));
				var text = String.Format("Период: {0} - {1}",
					model.Begin.Value.ToShortDateString(),
					model.End.Value.ToShortDateString());
				Header(text);
			}
			var headers = new[] {
				new PrintColumn("Наименование", 232),
				new PrintColumn("Производитель", 168),
				new PrintColumn("Прайс-лист", 132),
				new PrintColumn("Цена", 52),
				new PrintColumn("Заказ", 45),
				new PrintColumn("Сумма", 60)
			};

			int count;
			decimal sum;
			IEnumerable<object[]> rows;

			if (model.IsCurrentSelected) {
				var lines = model.GetItemsFromView<OrderLine>("Lines") ?? model.Lines.Value;
				count = lines.Count;
				sum = lines.Sum(l => l.MixedSum);
				rows = model.Lines.Value.Select(l => new object[] {
					l.ProductSynonym,
					l.ProducerSynonym,
					l.Order.PriceName,
					l.ResultCost,
					l.Count,
					l.MixedSum
				});
			}
			else {
				var lines = model.GetItemsFromView<SentOrderLine>("SentLines") ?? model.SentLines.Value;
				count = lines.Count;
				sum = lines.Sum(l => l.MixedSum);
				rows = model.SentLines.Value.Select(l => new object[] {
					l.ProductSynonym,
					l.ProducerSynonym,
					l.Order.PriceName,
					l.ResultCost,
					l.Count,
					l.MixedSum
				});
			}
			var table = BuildTable(rows, headers);
			if (count > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 1
						},
						new TableCell(new Paragraph(new Run("Позиций: " + count)) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 2
						},
						new TableCell(new Paragraph(new Run("Сумма: " + sum)) {
							KeepTogether = true
						}) {
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