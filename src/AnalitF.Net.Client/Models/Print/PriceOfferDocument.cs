using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	public class PriceOfferDocument
	{
		private List<Offer> offers;
		private Price price;
		private Address address;

		public PriceOfferDocument(List<Offer> offers, Price price, Address address)
		{
			this.offers = offers;
			this.price = price;
			this.address = address;
		}

		public FlowDocument BuildDocument()
		{
			var doc = new FlowDocument();
			doc.Blocks.Add(new Paragraph());

			var headerText = String.Format("Заявка на {0} от {1}", price.Name, address == null ? "" : address.Name);
			var header = new Table {
				Columns = {
					new TableColumn {
						Width = new GridLength(520)
					},
					new TableColumn {
						Width = GridLength.Auto
					},
				},
				RowGroups = {
					new TableRowGroup {
						Rows = {
							new TableRow {
								Cells = {
									new TableCell(new Paragraph(new Run(headerText)) {
										TextAlignment = TextAlignment.Left,
										FontWeight = FontWeights.Bold,
										FontSize = 16
									}),
									new TableCell(new Paragraph(new Run(price.Phone))) {
										TextAlignment = TextAlignment.Right
									}
								}
							}
						}
					}
				}
			};
			doc.Blocks.Add(header);

			var text = String.Format("Прайс-лист от {0}", price.PriceDate);
			doc.Blocks.Add(new Paragraph(new Run(text)));
			if (price.Order != null)
				doc.Blocks.Add(new Paragraph(new Run(price.Order.PersonalComment)));
			var headers = new [] {
				new PrintColumnDeclaration("№ п/п", 40),
				new PrintColumnDeclaration("Наименование", 260),
				new PrintColumnDeclaration("Производитель", 196),
				new PrintColumnDeclaration("Цена", 68),
				new PrintColumnDeclaration("Заказ", 40),
				new PrintColumnDeclaration("Сумма", 80)
			};
			var rows = offers.Select((o, i) => new object[]{
				++i,
				o.ProducerSynonym,
				o.ProducerSynonym,
				o.Cost,
				o.OrderLine == null ? null : (uint?)o.OrderLine.Count,
				o.OrderLine == null ? null : (decimal?)o.OrderLine.Sum,
			});
			var table = CatalogOfferDocument.BuildTable(rows, headers, offers.Count);
			var sum = offers.Where(o => o.OrderLine != null).Sum(o => o.OrderLine.Sum);
			var sumLabel = "";
			if (sum > 0)
				sumLabel = sum.ToString();
			table.RowGroups[0].Rows.Add(new TableRow {
				Cells = {
					new TableCell(new Paragraph(new Run("Итого: "))) {
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(1, 0, 0, 1),
						FontWeight = FontWeights.Bold,
						ColumnSpan = 2
					},
					new TableCell(new Paragraph(new Run("Позиций: " + offers.Count))) {
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(1, 0, 0, 1),
						FontWeight = FontWeights.Bold
					},
					new TableCell(new Paragraph(new Run("Сумма: " + sumLabel))) {
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(1, 0, 1, 1),
						FontWeight = FontWeights.Bold,
						ColumnSpan = 3
					}
				}
			});
			doc.Blocks.Add(table);

			return doc;
		}
	}
}