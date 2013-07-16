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

		public override FlowDocument Build()
		{
			var rows = offers.Select((o, i) => new object[] {
				o.ProductSynonym,
				o.ProducerSynonym,
				o.Price.Name,
				o.Period,
				o.Price.PriceDate,
				o.Diff,
				o.Cost
			});

			return BuildDocument(rows);
		}

		private FlowDocument BuildDocument(IEnumerable<object[]> rows)
		{
			var totalRows = offers.Count();
			var doc = new FlowDocument();

			doc.Blocks.Add(new Paragraph());
			doc.Blocks.Add(new Paragraph(new Run(reportHeader)) {
				FontWeight = FontWeights.Bold,
				FontSize = 16
			});

			var headers = new [] {
				new PrintColumnDeclaration("Наименование", 216),
				new PrintColumnDeclaration("Производитель", 136),
				new PrintColumnDeclaration("Прайс-лист", 112),
				new PrintColumnDeclaration("Срок год.", 85),
				new PrintColumnDeclaration("Дата пр.", 85),
				new PrintColumnDeclaration("Разн.", 48),
				new PrintColumnDeclaration("Цена", 55)
			};

			BuildTable(rows, headers);
			doc.Blocks.Add(new Paragraph(new Run(String.Format("Общее количество предложений: {0}", totalRows))));
			return doc;
		}
	}
}