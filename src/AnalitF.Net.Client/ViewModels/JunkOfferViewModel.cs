using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels
{
	public class JunkOfferViewModel : BaseOfferViewModel
	{
		public JunkOfferViewModel()
		{
			DisplayName = "Препараты с истекающими сроками годности";
			Update();
		}

		protected override void Query()
		{
			Offers = StatelessSession.Query<Offer>()
				.Where(o => o.Junk)
				.OrderBy(o => o.ProductSynonym)
				.Fetch(o => o.Price)
				.ToList();
		}
	}
}