using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class PriceViewModel : BaseScreen
	{
		private Price currentPrice;

		public PriceViewModel()
		{
			Prices = session.Query<Price>().OrderBy(c => c.Name).ToList();
		}

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

		public bool ShowLeaders { get; set; }

		public void EnterPrices()
		{
			if (CurrentPrice == null)
				return;

			Shell.ActiveAndSaveCurrent(new PriceOfferViewModel(CurrentPrice, ShowLeaders));
		}
	}
}