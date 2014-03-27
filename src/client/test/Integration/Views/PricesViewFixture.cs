using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using Common.Tools.Calendar;
using NUnit.Framework;
using Action = System.Action;
using WpfHelper = AnalitF.Net.Client.Test.TestHelpers.WpfHelper;

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
			WpfHelper.WithWindow(w => {
				var view = Bind(new PriceViewModel());

				var grid = (DataGrid)view.FindName("Prices");
				w.Loaded += (sender, args) => {
					var keyEventArgs = WpfHelper.KeyEventArgs(grid, Key.Enter);
					keyEventArgs.RoutedEvent = DataGrid.KeyDownEvent;
					grid.RaiseEvent(keyEventArgs);

					WpfHelper.Shutdown(w);
				};
				w.Content = view;
			});
			Assert.IsInstanceOf<PriceOfferViewModel>(shell.ActiveItem);
		}
	}
}