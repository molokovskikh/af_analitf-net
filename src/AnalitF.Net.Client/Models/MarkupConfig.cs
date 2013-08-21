using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{
	public enum MarkupType
	{
		Over,
		VitallyImportant
	}

	public class MarkupConfig : BaseNotify
	{
		private bool beginOverlap;
		private bool haveGap;
		private bool endLessThanBegin;

		public MarkupConfig()
		{}

		public MarkupConfig(decimal begin,
			decimal end,
			decimal markup,
			MarkupType type = MarkupType.Over)
		{
			Begin = begin;
			End = end;
			Markup = markup;
			MaxMarkup = markup;
			Type = type;
		}

		public virtual uint Id { get; set; }
		public virtual decimal Begin { get; set; }
		public virtual decimal End { get; set; }
		public virtual decimal Markup { get; set; }
		public virtual decimal MaxMarkup { get; set; }
		public virtual decimal? MaxSupplierMarkup { get; set; }
		public virtual MarkupType Type { get; set; }

		[Ignore]
		public virtual bool BeginOverlap
		{
			get
			{
				return beginOverlap;
			}
			set
			{
				beginOverlap = value;
				OnPropertyChanged("BeginOverlap");
			}
		}

		[Ignore]
		public virtual bool HaveGap
		{
			get
			{
				return haveGap;
			}
			set
			{
				haveGap = value;
				OnPropertyChanged("HaveGap");
			}
		}

		[Ignore]
		public virtual bool EndLessThanBegin
		{
			get { return endLessThanBegin; }
			set
			{
				endLessThanBegin = value;
				OnPropertyChanged("EndLessThanBegin");
			}
		}

		public static decimal Calculate(IList<MarkupConfig> markups, BaseOffer currentOffer)
		{
			if (currentOffer == null)
				return 0;

			var type = currentOffer.VitallyImportant ? MarkupType.VitallyImportant : MarkupType.Over;
			var cost = currentOffer.Cost;

			var config = Calculate(markups, type, cost);
			if (config == null)
				return 0;
			return config.Markup;
		}

		public static MarkupConfig Calculate(IEnumerable<MarkupConfig> markups, MarkupType type, decimal cost)
		{
			return markups
				.Where(m => m.Type == type)
				.Where(m => cost > m.Begin && cost <= m.End)
				.FirstOrDefault();
		}

		public static IEnumerable<MarkupConfig> Defaults()
		{
			return new[] {
				new MarkupConfig(0, 10000, 20),
				new MarkupConfig(0, 50, 20, MarkupType.VitallyImportant),
				new MarkupConfig(50, 500, 20, MarkupType.VitallyImportant),
				new MarkupConfig(500, 1000000, 20, MarkupType.VitallyImportant)
			};
		}

		public static Tuple<bool, string> Validate(IEnumerable<MarkupConfig> markups)
		{
			var data = markups.OrderBy(m => m.Begin).ToArray();
			foreach (var markup in data) {
				markup.EndLessThanBegin = markup.End < markup.Begin;
			}

			string validationError = null;
			var prev = data.First();
			foreach (var markup in data.Skip(1)) {
				markup.BeginOverlap = prev.End > markup.Begin;
				markup.HaveGap = prev.End < markup.Begin;
				if (markup.Markup > markup.MaxMarkup)
					validationError = "Максимальная наценка меньше наценки.";
				prev = markup;
			}

			var isValid = !data.Any(m => m.BeginOverlap || m.EndLessThanBegin || m.HaveGap)
				&& string.IsNullOrEmpty(validationError);
			return Tuple.Create(isValid, validationError);
		}

		public override string ToString()
		{
			return string.Format("{2}: {0} - {1} {3}%", Begin, End, Type, Markup);
		}
	}
}