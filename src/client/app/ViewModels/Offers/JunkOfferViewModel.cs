using System;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class JunkOfferViewModel : BaseOfferViewModel
	{
		public JunkOfferViewModel()
		{
			DisplayName = "Препараты с истекающими сроками годности";
			NavigateOnShowCatalog = true;

			QuickSearch = new QuickSearch<Offer>(UiScheduler,
				t => Offers.Value.FirstOrDefault(o => o.ProductSynonym.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0),
				CurrentOffer);
			QuickSearch.RemapChars = true;
			IsLoading = new NotifyValue<bool>(true);
		}

		public QuickSearch<Offer> QuickSearch { get; private set; }
		public NotifyValue<bool> IsLoading { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			RxQuery(s => {
					var offers = StatelessSession.Query<Offer>()
						.Where(o => o.Junk)
						.OrderBy(o => o.ProductSynonym)
						.Fetch(o => o.Price)
						.ToList();
					CalculateRetailCost(offers);
					LoadOrderItems(offers);
					return offers;
				})
				.ObserveOn(UiScheduler)
				.CatchSubscribe(o => {
					Offers.Value = o;
					SelectOffer();
					IsLoading.Value = false;
				}, CloseCancellation);
		}
	}
}