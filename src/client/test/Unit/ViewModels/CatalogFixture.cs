using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class CatalogFixture : BaseUnitFixture
	{
		[Test]
		public void Export()
		{
			var model = new CatalogViewModel();
			Activate(model);
			Assert.IsFalse(model.CanExport);

			user.Permissions.Add(new Permission("FPCN"));
			model = new CatalogViewModel();
			Activate(model);
			Assert.True(model.CanExport);
		}
	}
}