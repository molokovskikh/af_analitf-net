using System.Linq;
using System.Windows;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Views
{
	[TestFixture, RequiresSTA]
	public class CatalogViewFixture
	{
		private Client.Extentions.WindowManager manager;

		[SetUp]
		public void Setup()
		{
			manager = new Client.Extentions.WindowManager();
			manager.UnderTest = true;
		}

		[Test]
		public void Open_shell()
		{
			var view = new ShellView();
		}

		[Test]
		public void Show_catalog_view()
		{
			IoC.GetInstance = (type, key) => {
				return manager;
			};
			var view = new CatalogViewModel();
			view.CurrentCatalogName = view.CatalogNames.First();
			view.ShowDescription();
		}
	}
}