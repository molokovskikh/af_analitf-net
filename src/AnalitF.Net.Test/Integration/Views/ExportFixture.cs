using System;
using System.IO;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	public class BaseViewFixture : BaseFixture
	{
		[SetUp]
		public void BaseViewFixtureSetup()
		{
			ViewFixtureSetup.BindingErrors.Clear();
			ViewFixtureSetup.Setup();
		}

		[TearDown]
		public void TearDown()
		{
			if (ViewFixtureSetup.BindingErrors.Count > 0) {
				throw new Exception(ViewFixtureSetup.BindingErrors.Implode());
			}
		}

		protected T InitView<T>(BaseScreen model) where T : DependencyObject, new()
		{
			var view = ViewLocator.LocateForModel(model, null, null);
			ViewModelBinder.Bind(model, view, null);
			return view as T;
		}

		public static void ForceBinding(UIElement view)
		{
			var size = new Size(1000, 1000);
			view.Measure(size);
			view.Arrange(new Rect(size));
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