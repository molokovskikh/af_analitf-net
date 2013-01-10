using System.Linq;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture]
	public class BaseScreenFixture : BaseFixture
	{
		[Test]
		public void Close_screen_if_navigation_chain_not_empty()
		{
			var parent = new BaseScreen { DisplayName = "Каталог" };
			ScreenExtensions.TryActivate(parent);
			parent.NavigateBackward();

			parent.Parent = shell;
			shell.ActivateItem(parent);
			Assert.That(shell.ActiveItem, Is.EqualTo(parent));
			parent.NavigateBackward();
			Assert.That(shell.ActiveItem, Is.EqualTo(parent));

			var child = new BaseScreen { DisplayName = "Предложения" };
			shell.Navigate(child);
			Assert.That(shell.ActiveItem, Is.EqualTo(child));
			child.NavigateBackward();
			Assert.That(shell.ActiveItem, Is.EqualTo(parent));
			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(0));
		}

		[Test]
		public void Reactivate_view()
		{
			shell.ShowCatalog();
			var catalog = ((CatalogViewModel)shell.ActiveItem);
			var names = (CatalogNameViewModel)catalog.ActiveItem;
			names.CurrentCatalog = names.Catalogs[0];
			names.EnterCatalog();
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());

			shell.ShowCatalog();
			Assert.That(shell.NavigationStack.Count(), Is.EqualTo(0));
			Assert.That(shell.ActiveItem, Is.EqualTo(catalog));
		}
	}
}