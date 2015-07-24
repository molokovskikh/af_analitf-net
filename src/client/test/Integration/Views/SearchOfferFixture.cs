using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.Views.Offers;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class SearchOfferFixture : BaseViewFixture
	{
		[Test]
		public void Check_view()
		{
			var model = new SearchOfferViewModel();
			var view = (SearchOfferView)Bind(model);

			var offer = session.Query<Offer>().First();
			model.SearchBehavior.SearchText.Value = offer.ProductSynonym.Slice(3);
			model.SearchBehavior.Search();

			ForceBinding(view);
		}
	}
}