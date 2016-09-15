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
			IsLoading = new NotifyValue<bool>();
		}

		public QuickSearch<Offer> QuickSearch { get; }
		public NotifyValue<bool> IsLoading { get; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			DbReloadToken
				.Do(_ => IsLoading.Value = true)
				.SelectMany(_ => RxQuery(s => s.Query<Offer>()
					.Where(o => o.Junk)
					.OrderBy(o => o.ProductSynonym)
					.Fetch(o => o.Price)
					.ToList()
				))
				.ObserveOn(UiScheduler)
				.CatchSubscribe(o => {
					Calculate(o);
					LoadOrderItems(o);
					Offers.Value = o;
					SelectOffer();
					IsLoading.Value = false;
				}, CloseCancellation);
		}
	}
}