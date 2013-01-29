using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	[DataContract]
	public class PriceViewModel : BaseScreen
	{
		private Price currentPrice;
		private bool showLeader;

		public bool OpenSinglePrice;

		public PriceViewModel()
		{
			DisplayName = "Прайс-листы фирм";
			QuickSearch = new QuickSearch<Price>(
				t => Prices.FirstOrDefault(p => p.Name.ToLower().Contains(t)),
				p => CurrentPrice = p);
		}

		public QuickSearch<Price> QuickSearch { get; set; }

		public List<Price> Prices { get; set; }

		public Price CurrentPrice
		{
			get { return currentPrice; }
			set
			{
				currentPrice = value;
				NotifyOfPropertyChange("CurrentPrice");
			}
		}

		[DataMember]
		public bool ShowLeaders
		{
			get { return showLeader; }
			set
			{
				showLeader = value;
				NotifyOfPropertyChange("ShowLeaders");
			}
		}

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
					CurrentPrice = Prices.FirstOrDefault();
					EnterPrice();
				}
			}
		}

		public void SwitchShowLeaders()
		{
			ShowLeaders = !ShowLeaders;
		}

		private void Update()
		{
			var prices = Session.Query<Price>().OrderBy(c => c.Name).ToList();
			if (Address != null) {
				prices.Each(p => p.Order = Address.Orders.FirstOrDefault(o => o.Price == p));
				prices.Each(p => p.MinOrderSum = Address.Rules.FirstOrDefault(r => r.Price == p));
			}

			Prices = prices;
		}

		public void EnterPrice()
		{
			if (CurrentPrice == null)
				return;

			Shell.Navigate(new PriceOfferViewModel(CurrentPrice, ShowLeaders));
		}
	}
}