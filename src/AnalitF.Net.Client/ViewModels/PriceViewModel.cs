using System;
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
using Devart.Common;
using NHibernate;
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
				t => Prices.FirstOrDefault(p => p.Name.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0),
				p => CurrentPrice.Value = p);
		}

		public QuickSearch<Price> QuickSearch { get; set; }

		[Export]
		public List<Price> Prices { get; set; }

		[DataMember]
		public NotifyValue<Price> CurrentPrice { get; set; }

		[DataMember]
		public NotifyValue<bool> ShowLeaders { get; set; }

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

		public override void Update()
		{
			var prices = Session.Query<Price>().OrderBy(c => c.Name).ToList();
			if (Address != null) {
				Session.Evict(Address);
				Address = Session.Load<Address>(Address.Id);

				Price.LoadOrderStat(prices, Address, StatelessSession);
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

		public void EnterPrice()
		{
			if (CurrentPrice == null)
				return;

			Shell.Navigate(new PriceOfferViewModel(CurrentPrice.Value.Id, ShowLeaders));
		}
	}
}