using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class PriceOfferViewModel : BaseScreen
	{
		private Offer currentOffer;

		public PriceOfferViewModel(Price price)
		{
			Price = price;

			Offers = session.Query<Offer>().Where(o => o.PriceId == price.Id).ToList();
		}

		public Price Price { get; set; }

		public List<Offer> Offers { get; set; }

		public Offer CurrentOffer
		{
			get { return currentOffer; }
			set
			{
				currentOffer = value;
				RaisePropertyChangedEventImmediately("CurrentOffer");
			}
		}
	}
}