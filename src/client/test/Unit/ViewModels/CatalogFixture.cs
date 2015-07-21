using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
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

		[Test]
		public void Reset_term_on_show_with_same_mnn()
		{
			BaseScreen.TestQueryScheduler = new CurrentThreadTaskScheduler();
			var model = new CatalogViewModel();
			Activate(model);
			model.CatalogSearch.Value = true;
			model.SearchText = "тест";
			var search = ((CatalogSearchViewModel)model.ActiveItem);
			search.SearchBehavior.Search();
			search.CurrentCatalog.Value = new Catalog("тест") {
				Name = {
					Mnn = new Mnn()
				}
			};
			model.FilterByMnn = true;
			Assert.AreEqual("", search.SearchBehavior.ActiveSearchTerm.Value);
		}
	}
}