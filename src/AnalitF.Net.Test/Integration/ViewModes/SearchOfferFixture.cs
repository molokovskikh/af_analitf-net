using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class SearchOfferFixture : BaseFixture
	{
		[Test]
		public void Full_filter_values()
		{
			var model = new SearchOfferViewModel();
			Assert.That(model.Producers.Count, Is.GreaterThan(0));
			Assert.That(model.Prices.Count, Is.GreaterThan(0));
		}
	}
}