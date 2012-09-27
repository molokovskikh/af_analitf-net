using System.Linq;
using System.Windows;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using AnalitF.Net.Test.ViewModes;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Views
{
	[TestFixture, RequiresSTA]
	public class CatalogViewFixture : BaseFixture
	{
		[Test]
		public void Open_shell()
		{
			var view = new ShellView();
		}

		[Test]
		public void Show_catalog_view()
		{
			var view = new CatalogViewModel();
			view.CurrentCatalogName = view.CatalogNames.First();
			view.ShowDescription();
		}
	}
}