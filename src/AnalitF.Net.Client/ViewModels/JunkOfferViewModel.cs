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
			Offers = Session.Query<Offer>()
				.Where(o => o.Junk)
				.OrderBy(o => o.ProducerSynonym)
				.ToList();
			Calculate();
		}
	}
}