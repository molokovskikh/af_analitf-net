using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class OffersSearchFixture : BaseUnitFixture
	{
		[Test]
		public void Export()
		{
			user.Permissions.Clear();
			var model = new SearchOfferViewModel();
			Activate(model);
			Assert.IsFalse(model.CanExport);

			model.User.Permissions.Add(new Permission("FPL"));
			model = new SearchOfferViewModel();
			Activate(model);
			Assert.IsTrue(model.CanExport);
		}
	}
}