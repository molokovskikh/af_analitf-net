using System.Reactive.Disposables;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class CatalogNameFixture : BaseUnitFixture
	{
		[Test]
		public void Set_catalog()
		{
			var mode = new CatalogNameViewModel(new CatalogViewModel());
			mode.CurrentCatalog = new Catalog("тест");
			mode.CurrentCatalogName.Value = null;
			mode.CurrentCatalog = null;
		}

		[Test]
		public void Can_export()
		{
			user.Permissions.Clear();
			var mode = new CatalogNameViewModel(new CatalogViewModel());
			Assert.IsFalse(mode.CanExport);
		}

		[Test]
		public void Recalc_can_export()
		{
			user.Permissions.Add(new Permission("FPCF"));
			var model = new CatalogNameViewModel(new CatalogViewModel());
			Activate(model);
			Assert.IsFalse(model.CanExport);
			model.ActivateCatalog();
			Assert.IsTrue(model.CanExport);
		}
	}
}