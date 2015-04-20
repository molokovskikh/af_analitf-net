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
		private IList<Offer> offers;
		private string header;

		public CatalogOfferDocument(string header, IList<Offer> offers)
		{
			this.header = header;
			this.offers = offers;
		}

		protected override void BuildDoc()
		{
			doc.Blocks.Add(new Paragraph());
			doc.Blocks.Add(new Paragraph(new Run(header)) {
				FontWeight = FontWeights.Bold,
				FontSize = 16
			});

			var headers = new[] {
				new PrintColumn("Наименование", 210),
				new PrintColumn("Производитель", 120),
				new PrintColumn("Прайс-лист", 110),
				new PrintColumn("Срок год.", 80),
				new PrintColumn("Дата пр.", 80),
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
				o.ResultCost
			});

			BuildTable(rows, headers);
			doc.Blocks.Add(new Paragraph(new Run(String.Format("Общее количество предложений: {0}", offers.Count()))));
		}
	}
}