﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	[DataContract]
	public class PriceViewModel : BaseScreen
	{
		public bool OpenSinglePrice;

		public PriceViewModel()
		{
			DisplayName = "Прайс-листы фирм";
			CurrentPrice = new NotifyValue<Price>();
			ShowLeaders = new NotifyValue<bool>();
			QuickSearch = new QuickSearch<Price>(UiScheduler,
				t => Prices.FirstOrDefault(p => p.Name.ToLower().Contains(t)),
				p => CurrentPrice.Value = p);
		}

		public QuickSearch<Price> QuickSearch { get; set; }

		[Export]
		public List<Price> Prices { get; set; }

		[DataMember]
		public NotifyValue<Price> CurrentPrice { get; set; }

		[DataMember]
		public NotifyValue<bool> ShowLeaders { get; set; }

		//при активации надо обновить данные тк можно войти в прайс, сделать заказ а потом вернуться
		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
		}

		public override void PostActivated()
		{
			if (OpenSinglePrice) {
				OpenSinglePrice = false;
				if (Prices.Count == 1 && Prices[0].PositionCount > 0) {
					CurrentPrice.Value = Prices.FirstOrDefault();
					EnterPrice();
				}
			}
		}

		public void SwitchShowLeaders()
		{
			ShowLeaders.Value = !ShowLeaders.Value;
		}

		private void Update()
		{
			var prices = Session.Query<Price>().OrderBy(c => c.Name).ToList();
			if (Address != null) {
				Session.Evict(Address);
				Address = Session.Load<Address>(Address.Id);

				var weekBegin = DateTime.Today.FirstDayOfWeek();
				var monthBegin = DateTime.Today.FirstDayOfMonth();
				var monthlyStat = OrderStat(monthBegin);
				var weeklyStat = OrderStat(weekBegin);

				prices.Each(p => p.WeeklyOrderSum = weeklyStat.Where(s => s.Item1 == p.Id)
					.Select(s => (decimal?)s.Item2).FirstOrDefault());
				prices.Each(p => p.MonthlyOrderSum = monthlyStat.Where(s => s.Item1 == p.Id)
					.Select(s => (decimal?)s.Item2).FirstOrDefault());
				prices.Each(p => p.Order = Address.Orders.Where(o => !o.Frozen).FirstOrDefault(o => o.Price == p));
				prices.Each(p => p.MinOrderSum = Address.Rules.FirstOrDefault(r => r.Price == p));
			}

			Prices = prices;
			if (CurrentPrice.Value != null) {
				CurrentPrice.Value = Prices.Where(p => p.Id == CurrentPrice.Value.Id)
					.DefaultIfEmpty(Prices.FirstOrDefault())
					.First();
			}
		}

		private List<System.Tuple<PriceComposedId, decimal>> OrderStat(DateTime from)
		{
			var addressId = Address.Id;

			return StatelessSession.Query<SentOrder>()
				.Where(o => o.Address.Id == addressId && o.SentOn >= from)
				.GroupBy(o => o.Price)
				.Select(g => Tuple.Create(g.Key.Id, g.Sum(o => o.Sum)))
				.ToList();
		}

		public void EnterPrice()
		{
			if (CurrentPrice == null)
				return;

			Shell.Navigate(new PriceOfferViewModel(CurrentPrice.Value.Id, ShowLeaders));
		}
	}
}