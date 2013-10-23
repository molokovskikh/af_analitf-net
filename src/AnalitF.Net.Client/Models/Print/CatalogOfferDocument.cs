using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	public class CatalogOfferDocument : BaseDocument
	{
		private List<Offer> offers;
		private string reportHeader;

		public CatalogOfferDocument(List<Offer> offers, string header)
		{
			this.offers = offers;
			reportHeader = header;
		}

		protected override void BuildDoc()
		{
			doc.Blocks.Add(new Paragraph());
			doc.Blocks.Add(new Paragraph(new Run(reportHeader)) {
				FontWeight = FontWeights.Bold,
				FontSize = 16
			});

			var headers = new [] {
				new PrintColumn("Наименование", 216),
				new PrintColumn("Производитель", 136),
				new PrintColumn("Прайс-лист", 112),
				new PrintColumn("Срок год.", 85),
				new PrintColumn("Дата пр.", 85),
				new PrintColumn("Разн.", 48),
				new PrintColumn("Цена", 55)
			};
			var rows = offers.Select((o, i) => new object[] {
				o.ProductSynonym,
				o.ProducerSynonym,
				o.Price.Name,
				o.Period,
				o.Price.PriceDate,
				o.Diff,
				o.Cost
			});

			BuildTable(rows, headers);
			doc.Blocks.Add(new Paragraph(new Run(String.Format("Общее количество предложений: {0}", offers.Count()))));
		}
	}
}