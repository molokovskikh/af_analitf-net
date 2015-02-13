using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class JunkOffersFixture : BaseUnitFixture
	{
		[Test]
		public void Disabled_export()
		{
			user.Permissions.Clear();
			var model = new JunkOfferViewModel();
			Activate(model);
			Assert.IsFalse(model.CanExport);

			model.User.Permissions.Add(new Permission("EPP"));
			model = new JunkOfferViewModel();
			Activate(model);
			Assert.IsTrue(model.CanExport);
		}
	}
}