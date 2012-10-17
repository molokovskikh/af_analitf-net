using System.Collections.Generic;
using System.Linq;

namespace AnalitF.Net.Client.Models
{
	public class MarkupConfig
	{
		public MarkupConfig()
		{}

		public MarkupConfig(decimal begin, decimal end, decimal markup)
		{
			Begin = begin;
			End = end;
			Markup = markup;
		}

		public virtual uint Id { get; set; }
		public virtual decimal Begin { get; set; }
		public virtual decimal End { get; set; }
		public virtual decimal Markup { get; set; }

		public static decimal Calculate(List<MarkupConfig> markups, BaseOffer currentOffer)
		{
			if (currentOffer == null)
				return 0;

			return markups.Where(m => currentOffer.Cost > m.Begin && currentOffer.Cost <= m.End)
				.Select(m => m.Markup)
				.FirstOrDefault();
		}

		public static IEnumerable<MarkupConfig> Defaults()
		{
			return new[] {
				new MarkupConfig(0, 10000, 20)
			};
		}
	}
}