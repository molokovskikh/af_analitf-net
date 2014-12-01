using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class PricesViewFixture : BaseViewFixture
	{
		[Test]
		public void Bind_quick_search()
		{
			var view = Bind(new PriceViewModel());

			var search = view.Descendants<Control>().First(e => e.Name == "QuickSearch");
			var text = search.Descendants<TextBox>().FirstOrDefault();

			Assert.That(text, Is.Not.Null);
		}

		[Test]
		public void Enter_price()
		{
			WpfTestHelper.WithWindow2(async w => {
				var view = Bind(new PriceViewModel());
				var grid = (DataGrid)view.FindName("Prices");
				w.Content = view;

				await w.WaitLoaded();
				var keyEventArgs = WpfTestHelper.KeyEventArgs(grid, Key.Enter);
				keyEventArgs.RoutedEvent = DataGrid.KeyDownEvent;
				grid.RaiseEvent(keyEventArgs);
			});
			Assert.IsInstanceOf<PriceOfferViewModel>(shell.ActiveItem);
		}
	}
}