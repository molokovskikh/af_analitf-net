using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	public class PriceOfferDocument : BaseDocument
	{
		private IList<Offer> offers;
		private Price price;
		private Address address;

		public PriceOfferDocument(IList<Offer> offers, Price price, Address address)
		{
			this.offers = offers;
			this.price = price;
			this.address = address;
		}

		protected override void BuildDoc()
		{
			doc.Blocks.Add(new Paragraph());

			var headerText = String.Format("Заявка на {0} от {1}", price.Name, address == null ? "" : address.Name);
			TwoColumnHeader(headerText, price.Phone);

			var text = String.Format("Прайс-лист от {0}", price.PriceDate);
			doc.Blocks.Add(new Paragraph(new Run(text)));
			if (price.Order != null)
				doc.Blocks.Add(new Paragraph(new Run(price.Order.PersonalComment)));
			var headers = new[] {
				new PrintColumn("№ п/п", 45),
				new PrintColumn("Наименование", 260),
				new PrintColumn("Производитель", 196),
				new PrintColumn("Цена", 68),
				new PrintColumn("Заказ", 45),
				new PrintColumn("Сумма", 80)
			};
			var rows = offers.Select((o, i) => new object[]{
				i + 1,
				o.ProductSynonym,
				o.ProducerSynonym,
				o.ResultCost,
				o.OrderLine == null ? null : (uint?)o.OrderLine.Count,
				o.OrderLine == null ? null : (decimal?)o.OrderLine.ResultSum,
			});
			var table = BuildTable(rows, headers);
			var sum = offers.Where(o => o.OrderLine != null).Sum(o => o.OrderLine.ResultSum);
			var sumLabel = "";
			if (sum > 0)
				sumLabel = sum.ToString();
			table.RowGroups[0].Rows.Add(new TableRow {
				Cells = {
					new TableCell(new Paragraph(new Run("Итого: ")) {
						KeepTogether = true
					}) {
						Style = CellStyle,
						FontWeight = FontWeights.Bold,
						ColumnSpan = 2
					},
					new TableCell(new Paragraph(new Run("Позиций: " + offers.Count)) {
						KeepTogether = true
					}) {
						Style = CellStyle,
						FontWeight = FontWeights.Bold
					},
					new TableCell(new Paragraph(new Run("Сумма: " + sumLabel)) {
						KeepTogether = true
					}) {
						Style = CellStyle,
						FontWeight = FontWeights.Bold,
						ColumnSpan = 3,
						TextAlignment = TextAlignment.Right
					}
				}
			});
			doc.Blocks.Add(table);
		}
	}
}