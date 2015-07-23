using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class CatalogSearchFixture : BaseViewFixture
	{
		[Test]
		public void Mark_search_results()
		{
			WpfTestHelper.WithWindow2(async w => {
				var model = new CatalogViewModel();
				var view = Bind(model);
				w.Content = view;

				await view.WaitLoaded();

				var searchCheck = view.Descendants<CheckBox>().First(c => c.Name == "CatalogSearch");
				searchCheck.IsChecked = true;
				await view.WaitIdle();

				var allCheck = view.Descendants<CheckBox>().First(c => c.Name == "ShowWithoutOffers");
				allCheck.IsChecked = true;
				await view.WaitIdle();

				var search = view.Descendants<TextBox>().First(b => b.Name == "SearchText");
				search.Text = "мыло";
				var grid = view.Descendants<DataGrid>().First(g => g.Name == "Items");
				grid.SendKey(Key.Return);
				await view.WaitIdle();
			});
		}

		[Test]
		public void Quick_search()
		{
			var term = session.Query<Catalog>().First(c => c.HaveOffers).FullName.Substring(0, 1);
			WpfTestHelper.WithWindow2(async w => {
				var model = new CatalogViewModel();
				var view = Bind(model);
				w.Content = view;

				await view.WaitLoaded();

				var searchCheck = view.Descendants<CheckBox>().First(c => c.Name == "CatalogSearch");
				searchCheck.IsChecked = true;
				await view.WaitIdle();
				scheduler.Start();

				var grid = view.Descendants<DataGrid>().First(g => g.Name == "Items");
				grid.SendText(term);
				var search = view.Descendants<TextBox>().First(b => b.Name == "SearchText");
				Assert.IsTrue(search.IsVisible);
				Assert.AreEqual(term, search.Text);
				search.Text = "";

				var enable = view.Descendants<CheckBox>().First(c => c.Name == "IsQuickSearchEnabled");
				enable.IsChecked = true;

				grid.SendText(term);
				Assert.AreEqual("", search.Text);
				var quicksearch = view.Descendants<ContentControl>().First(c => c.Name == "QuickSearch")
					.Descendants<TextBox>().First(c => c.Name == "SearchText");
				Assert.AreEqual(term, quicksearch.Text, quicksearch.GetHashCode().ToString());
			});
		}
	}
}