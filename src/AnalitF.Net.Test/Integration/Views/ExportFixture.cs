using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	public class BaseViewFixture : BaseFixture
	{
		[SetUp]
		public void BaseViewFixtureSetup()
		{
			ViewFixtureSetup.Setup();
		}
	}

	[TestFixture, RequiresSTA]
	public class ExportFixture : BaseViewFixture
	{
		[Test]
		public void Export()
		{
			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var model = Init(new CatalogOfferViewModel(catalog));
			((IViewAware)model).AttachView(new CatalogOfferView());

			Assert.That(model.CanExport, Is.True);
			var result = (OpenFileResult)model.Export();
			Assert.That(File.Exists(result.Filename), result.Filename);
			File.Delete(result.Filename);
		}
	}
}