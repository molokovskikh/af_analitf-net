using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class ExportFixture : BaseViewFixture
	{
		[Test]
		public void Export()
		{
			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var model = new CatalogOfferViewModel(catalog);
			CheckExport(model);
		}

		[Test]
		public void Export_prices()
		{
			var model = new PriceViewModel();
			CheckExport(model);
		}

		private void CheckExport(BaseScreen model)
		{
			Init(model);
			InitView(model);

			Assert.That(model.CanExport, Is.True);
			var result = (OpenResult)model.Export();
			Assert.That(File.Exists(result.Filename), result.Filename);
			File.Delete(result.Filename);
		}
	}
}