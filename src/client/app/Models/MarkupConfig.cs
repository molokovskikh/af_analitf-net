using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NHibernate.Util;

namespace AnalitF.Net.Client.Models
{
	public enum MarkupType
	{
		Over,
		VitallyImportant,
		Nds18,
		Special
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

		public MarkupConfig(MarkupConfig config, Address address)
		{
			Begin = config.Begin;
			End = config.End;
			Type = config.Type;
			Markup = config.Markup;
			MaxMarkup = config.MaxMarkup;
			MaxSupplierMarkup = config.MaxSupplierMarkup;
			Address = address;
			Settings = config.Settings;
		}

		public MarkupConfig(Address address,
			decimal begin,
			decimal end,
			decimal markup,
			MarkupType type = MarkupType.Over)
		{
			Address = address;
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

		public virtual Address Address { get; set; }
		public virtual decimal Markup { get; set; }
		public virtual decimal MaxMarkup { get; set; }
		public virtual decimal MaxSupplierMarkup { get; set; }
		public virtual MarkupType Type { get; set; }
		public virtual Settings Settings { get; set; }

		private void Validate()
		{
			Settings?.Validate();
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

		public static decimal Calculate(IEnumerable<MarkupConfig> markups, BaseOffer offer, User user, Address address)
		{
			if (offer == null)
				return 0;
			if (address == null)
				return 0;

			var type = MarkupType.Over;
			if (offer.NDS == 18)
				type = MarkupType.Nds18;
			else if (offer.VitallyImportant)
				type = MarkupType.VitallyImportant;
			if (offer.IsSpecialMarkup)
				type = MarkupType.Special;

			var cost = user.IsDelayOfPaymentEnabled && !user.ShowSupplierCost ? offer.GetResultCost() : offer.Cost;

			return (Calculate(markups, type, cost, address)?.Markup).GetValueOrDefault();
		}

		public static MarkupConfig Calculate(IEnumerable<MarkupConfig> markups, MarkupType type, decimal cost, Address address)
		{
			return markups.OrderBy(s => s.Begin)
				.FirstOrDefault(x => x.Type == type
					&& x.Address.Id == address.Id
					&& cost > x.Begin
					&& cost <= x.End);
		}

		public static IEnumerable<MarkupConfig> Defaults(Address address)
		{
			return new[] {
				new MarkupConfig(address, 0, 10000, 20),
				new MarkupConfig(address, 0, 10000, 20, MarkupType.Nds18),
				new MarkupConfig(address, 0, 50, 20, MarkupType.VitallyImportant),
				new MarkupConfig(address, 50, 500, 20, MarkupType.VitallyImportant),
				new MarkupConfig(address, 500, 1000000, 20, MarkupType.VitallyImportant),
				new MarkupConfig(address, 0, 10000, 20, MarkupType.Special),
			};
		}

		public static List<string[]> Validate(IEnumerable<MarkupConfig> source)
		{
			var groups = source.GroupBy(c => new { c.Type, c.Address?.Id });
			var errors = new List<string[]>();
			foreach (var markups in groups) {
				var data = markups.OrderBy(m => m.Begin).ToArray();
				markups.Each(x => {
					x.HaveGap = false;
					x.EndLessThanBegin = false;
					x.BeginOverlap = false;
				});
				foreach (var markup in data) {
					markup.EndLessThanBegin = markup.End < markup.Begin;
				}

				var prev = data.First();
				foreach (var markup in data.Skip(1)) {
					markup.BeginOverlap = prev.End > markup.Begin;
					markup.HaveGap = prev.End < markup.Begin;
					if (markup.Markup > markup.MaxMarkup) {
						errors.Add(new string[]{ markup.Type.ToString(), "Максимальная наценка меньше наценки." });
					}

					if (markup.BeginOverlap || markup.EndLessThanBegin || markup.HaveGap) {
						errors.Add(new string[]{ markup.Type.ToString(), "Некорректно введены границы цен." });
					}
					prev = markup;
				}
			}
			var ranges = source.Where(m => m.Type == MarkupType.VitallyImportant).Select(m => m.Begin);
			if (ranges.Intersect(new decimal[] { 0, 50, 500 }).Count() < 3)
				errors.Add(new string[] { MarkupType.VitallyImportant.ToString(), "Не заданы обязательные интервалы границ цен: [0, 50], [50, 500], [500, 1000000]." });

			if (errors.Count == 0) {
				errors = null;
			}

			return errors;
		}

		public override string ToString()
		{
			return string.Format("{2}: {0} - {1} {3}%", Begin, End, Type, Markup);
		}
	}
}