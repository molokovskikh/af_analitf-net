﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Common.Tools.Calendar;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models
{
	public class DelayOfPayment
	{
		public DelayOfPayment()
		{
		}

		public DelayOfPayment(decimal value, Price price)
		{
			DayOfWeek = DateTime.Today.DayOfWeek;
			OtherDelay = value;
			Price = price;
		}

		public DelayOfPayment(DayOfWeek day, decimal value)
		{
			DayOfWeek = day;
			OtherDelay = value;
		}

		public virtual uint Id { get; set; }

		public virtual DayOfWeek DayOfWeek { get; set; }

		public virtual decimal OtherDelay { get; set; }

		public virtual decimal VitallyImportantDelay { get; set; }

		//todo! приводит к ошибке сериализации
		public virtual Price Price { get; set; }

		public override string ToString()
		{
			return string.Format("{0} - {1} | {2}", DayOfWeek, OtherDelay, VitallyImportantDelay);
		}
	}

	[Serializable]
	public class PriceComposedId : IEquatable<PriceComposedId>, IComparable
	{
		public uint PriceId { get; set; }

		public ulong RegionId { get; set; }

		public bool Equals(PriceComposedId other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return RegionId == other.RegionId && PriceId == other.PriceId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((PriceComposedId)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return (RegionId.GetHashCode() * 397) ^ (int)PriceId;
			}
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
				return 1;

			var id = obj as PriceComposedId;
			if (id == null)
				throw new Exception(String.Format("Unexpected object type {0} {1}", obj, obj.GetType()));

			if (PriceId != id.PriceId)
				if (PriceId > id.PriceId)
					return 1;
				else
					return -1;

			if (RegionId != id.RegionId)
				if (RegionId > id.RegionId)
					return 1;
				else
					return -1;

			return 0;
		}

		public static bool operator ==(PriceComposedId v1, PriceComposedId v2) {
			return Equals(v1, v2);
		}

		public static bool operator !=(PriceComposedId v1, PriceComposedId v2) {
			return !Equals(v1, v2);
		}

		public override string ToString()
		{
			return string.Format("PriceId: {0}, RegionId: {1}", PriceId, RegionId);
		}
	}

	public class Mailto
	{
		public Mailto(string mailto, string name)
		{
			Uri = mailto;
			Name = name;
		}

		public string Uri { get; set; }
		public string Name { get; set; }

		public override string ToString()
		{
			return string.Format("{0} {1}", Uri, Name);
		}
	}

	public class Price : BaseNotify
	{
		private Order order;

		public Price(string name) : this()
		{
			Id = new PriceComposedId();
			Name = name;
		}

		public Price()
		{
			CostFactor = 1;
			VitallyImportantCostFactor = 1;
		}

		public virtual PriceComposedId Id { get; set; }

		/// <summary>
		/// название которое отображается в интерфейсе, зависит от опции "Всегда показывать названия прайс-листов"
		/// </summary>
		public virtual string Name { get; set; }

		/// <summary>
		/// Название прайса без названия поставщика, нужно для вычисления, Name
		/// не использовать, используй Name
		/// </summary>
		public virtual string PriceName { get; set; }

		public virtual uint? CostId { get; set; }

		public virtual string CostName { get; set; }

		public virtual ulong RegionId { get; set; }

		public virtual string RegionName { get; set; }

		public virtual uint SupplierId { get; set; }

		public virtual string SupplierName { get; set; }

		public virtual string SupplierFullName { get; set; }

		public virtual bool Storage { get; set; }

		public virtual uint PositionCount { get; set; }

		public virtual DateTime PriceDate { get; set; }

		public virtual string OperativeInfo { get; set; }

		public virtual string ContactInfo { get; set; }

		public virtual string Phone { get; set; }

		public virtual string Email { get; set; }

		public virtual bool BasePrice { get; set; }

		public virtual int Category { get; set; }

		public virtual bool DisabledByClient { get; set; }

		public virtual DateTime? Timestamp { get; set; }

		//корректировка цен применяемая аптекой, обновляются при запуске программы
		public virtual decimal CostFactor { get; set; }

		//корректировка цен применяемая аптекой, для жизненно важных препаратов
		public virtual decimal VitallyImportantCostFactor { get; set; }

		//экспортировался ли прайс-лист при последнем обновлении
		public virtual bool IsSynced { get; set; }

		//формирование заявок запрещено
		public virtual bool IsOrderDisabled { get; set; }

		[Style("Name")]
		public virtual bool NotBase
		{
			get { return !BasePrice; }
		}

		[Ignore]
		public virtual bool Active
		{
			get { return !DisabledByClient; }
			set
			{
				DisabledByClient = !value;
				OnPropertyChanged();
			}
		}

		[Ignore]
		public virtual decimal? WeeklyOrderSum { get; set; }

		[Ignore]
		public virtual decimal? MonthlyOrderSum { get; set; }

		[Ignore]
		public virtual List<Mailto> Emails {
			get
			{
				if (String.IsNullOrEmpty(Email))
					return new List<Mailto>();
				var parts = Email.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(p => p.Trim())
					.Where(p => !String.IsNullOrWhiteSpace(p))
					.ToArray();
				if (parts.Length == 0)
					return new List<Mailto>();
				var uri = "mailto:" + parts.Implode(",");
				return parts.Select(v => new Mailto(uri, v)).ToList();
			}
		}

		[Ignore, JsonIgnore]
		public virtual Order Order
		{
			get { return order; }
			set
			{
				order = value;
				OnPropertyChanged();
			}
		}

		[Ignore, Style]
		public virtual bool HaveOrder
		{
			get { return Order != null; }
		}

		[Ignore, JsonIgnore]
		public virtual MinOrderSumRule MinOrderSum { get; set; }

		private static List<Tuple<PriceComposedId, decimal>> OrderStat(DateTime from,
			Address address, IStatelessSession session)
		{
			var addressId = address.Id;
			//g.Key != null ? g.Key.Id : null - на первый взгляд это выражение не имеет смысла
			//но в этом методе происходит ошибка NRE единственное что подходит под это g.Key.Id
			return session.Query<SentOrder>()
				.Where(o => o.Address.Id == addressId && o.SentOn >= @from)
				.GroupBy(o => o.Price)
				.Select(g => Tuple.Create(g.Key != null ? g.Key.Id : null, g.Sum(o => o.Sum)))
				.ToList()
				.Where(t => t.Item1 != null)
				.ToList();
		}

		public static void LoadOrderStat(IEnumerable<Price> prices, Address address, IStatelessSession session)
		{
			if (address == null)
				return;
			if (session == null)
				return;

			var weekBegin = DateTime.Today.FirstDayOfWeek();
			var monthBegin = DateTime.Today.FirstDayOfMonth();
			var monthlyStat = OrderStat(monthBegin, address, session);
			var weeklyStat = OrderStat(weekBegin, address, session);

			prices.Each(p => {
				//у нас могут быть заказы без прайс листов
				//ловим исключения для таких заказов
				try {
					p.WeeklyOrderSum = weeklyStat.Where(s => s.Item1 == p.Id)
						.Select(s => (decimal?)s.Item2).FirstOrDefault();
					p.MonthlyOrderSum = monthlyStat.Where(s => s.Item1 == p.Id)
						.Select(s => (decimal?)s.Item2).FirstOrDefault();
				} catch(ObjectNotFoundException) {  }
			});
		}

		public override string ToString()
		{
			return Name;
		}
	}
}