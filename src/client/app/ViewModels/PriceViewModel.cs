using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	[DataContract]
	public class PriceViewModel : BaseScreen
	{
		public bool OpenSinglePrice;

		public PriceViewModel()
		{
			DisplayName = "Прайс-листы фирм";
			InitFields();
			Prices.Value = new List<Price>();
			QuickSearch = new QuickSearch<Price>(UiScheduler,
				t => Prices.Value.FirstOrDefault(p => p.Name.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0),
				CurrentPrice);
		}

		public QuickSearch<Price> QuickSearch { get; set; }

		[Export]
		public NotifyValue<List<Price>> Prices { get; set; }

		[DataMember]
		public NotifyValue<Price> CurrentPrice { get; set; }

		[DataMember]
		public NotifyValue<bool> ShowLeaders { get; set; }

		public override void PostActivated()
		{
			if (OpenSinglePrice) {
				OpenSinglePrice = false;
				if (Prices.Value.Count == 1 && Prices.Value[0].PositionCount > 0) {
					CurrentPrice.Value = Prices.Value.FirstOrDefault();
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
			if (Session == null)
				return;
			var prices = Session.Query<Price>().OrderBy(c => c.Name).ToList();
			if (Address != null) {
				Price.LoadOrderStat(Env, prices, Address).LogResult();
				prices.Each(p => p.Order = Address.Orders.Where(o => !o.Frozen).FirstOrDefault(o => o.Price == p));
				prices.Each(p => p.MinOrderSum = Address.Rules.FirstOrDefault(r => r.Price == p));
			}

			prices.Select(p => p.ObservableForProperty(x => x.Active))
				.Merge()
				.Throttle(TimeSpan.FromMilliseconds(1000), UiScheduler)
				.Subscribe(_ => ResultsSink.OnNext(MessageResult.Warn("Изменение настроек прайс-листов будет применено при следующем обновлении.")), CloseCancellation.Token);

			Prices.Value = prices;
			if (CurrentPrice.Value != null) {
				CurrentPrice.Value = Prices.Value.Where(p => p.Id == CurrentPrice.Value.Id)
					.DefaultIfEmpty(Prices.Value.FirstOrDefault())
					.First();
			}
		}

		public void EnterPrice()
		{
			if (CurrentPrice.Value == null || !CurrentPrice.Value.Active)
				return;

			Shell.Navigate(new PriceOfferViewModel(CurrentPrice.Value.Id, ShowLeaders));
		}
	}
}