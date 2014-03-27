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
		private decimal begin;
		private decimal end;

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

		public virtual decimal Begin
		{
			get { return begin; }
			set
			{
				begin = value;
				Validate();
			}
		}

		public virtual decimal End
		{
			get { return end; }
			set
			{
				end = value;
				Validate();
			}
		}

		public virtual decimal Markup { get; set; }
		public virtual decimal MaxMarkup { get; set; }
		public virtual decimal? MaxSupplierMarkup { get; set; }
		public virtual MarkupType Type { get; set; }
		public virtual Settings Settings { get; set; }

		private void Validate()
		{
			if (Settings == null)
				return;

			Settings.ValidateMarkups();
		}

		[Ignore, Style("Begin")]
		public virtual bool BeginOverlap
		{
			get
			{
				return beginOverlap;
			}
			set
			{
				if (beginOverlap == value)
					return;

				beginOverlap = value;
				OnPropertyChanged();
			}
		}

		[Ignore, Style("Begin")]
		public virtual bool HaveGap
		{
			get
			{
				return haveGap;
			}
			set
			{
				if (haveGap == value)
					return;

				haveGap = value;
				OnPropertyChanged();
			}
		}

		[Ignore, Style("End")]
		public virtual bool EndLessThanBegin
		{
			get { return endLessThanBegin; }
			set
			{
				if (endLessThanBegin == value)
					return;
				endLessThanBegin = value;
				OnPropertyChanged();
			}
		}

		public static decimal Calculate(IEnumerable<MarkupConfig> markups, BaseOffer offer, User user)
		{
			if (offer == null)
				return 0;

			var type = offer.VitallyImportant ? MarkupType.VitallyImportant : MarkupType.Over;
			var cost = user.IsDeplayOfPaymentEnabled && !user.ShowSupplierCost ? offer.GetResultCost() : offer.Cost;

			var config = Calculate(markups, type, cost);
			if (config == null)
				return 0;
			return config.Markup;
		}

		public static MarkupConfig Calculate(IEnumerable<MarkupConfig> markups, MarkupType type, decimal cost)
		{
			return markups
				.Where(m => m.Type == type)
				.FirstOrDefault(m => cost > m.Begin && cost <= m.End);
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

		public static string Validate(IEnumerable<MarkupConfig> source)
		{
			var groups = source.GroupBy(c => c.Type);
			var errors = new List<string>();
			foreach (var markups in groups) {
				var data = markups.OrderBy(m => m.Begin).ToArray();
				foreach (var markup in data) {
					markup.EndLessThanBegin = markup.End < markup.Begin;
				}

				var prev = data.First();
				foreach (var markup in data.Skip(1)) {
					markup.BeginOverlap = prev.End > markup.Begin;
					markup.HaveGap = prev.End < markup.Begin;
					if (markup.Markup > markup.MaxMarkup) {
						errors.Add("Максимальная наценка меньше наценки.");
					}
					prev = markup;
				}

				if (data.Any(m => m.BeginOverlap || m.EndLessThanBegin || m.HaveGap)) {
					errors.Add("Некорректно введены границы цен.");
				}
			}
			return errors.FirstOrDefault();
		}

		public override string ToString()
		{
			return string.Format("{2}: {0} - {1} {3}%", Begin, End, Type, Markup);
		}
	}
}