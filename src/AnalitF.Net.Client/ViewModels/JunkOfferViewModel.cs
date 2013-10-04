using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class JunkOfferViewModel : BaseOfferViewModel
	{
		public JunkOfferViewModel()
		{
			DisplayName = "Препараты с истекающими сроками годности";
			NavigateOnShowCatalog = true;

			QuickSearch = new QuickSearch<Offer>(UiScheduler,
				t => Offers.Value.FirstOrDefault(o => o.ProductSynonym.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0),
				o => CurrentOffer = o);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Update();
		}

		public QuickSearch<Offer> QuickSearch { get; private set; }

		protected override void Query()
		{
			Offers.Value = StatelessSession.Query<Offer>()
				.Where(o => o.Junk)
				.OrderBy(o => o.ProductSynonym)
				.Fetch(o => o.Price)
				.ToList();
		}
	}
}