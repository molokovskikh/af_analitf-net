using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.ViewModes
{
	[TestFixture]
	public class SearchOfferViewModelFixture
	{
		[Test]
		public void Full_filter_values()
		{
			SearchOfferViewModel model = null;
			Assert.That(model.Producers.Count, Is.GreaterThan(0));
			Assert.That(model.Prices.Count, Is.GreaterThan(0));
		}
	}
}