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

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class PricesViewFixture : BaseViewFixture
	{
		[Test]
		public void Bind_quick_search()
		{
			var view = Bind(new PriceViewModel());

			var search = view.DeepChildren().OfType<Control>().First(e => e.Name == "QuickSearch");
			var text = search.DeepChildren().OfType<TextBox>().FirstOrDefault();

			Assert.That(text, Is.Not.Null);
		}

		[Test]
		public void Enter_price()
		{
			WpfHelper.WithWindow(w => {
				var view = Bind(new PriceViewModel());

				var grid = (DataGrid)view.FindName("Prices");
				w.Loaded += (sender, args) => {
					IDisposable subscription = null;
					//ловим изменение ActiveItems
					//нужно что бы wpf успел сделать какие то свои магичекские дела
					//если не ждать тогда nre
					subscription = shell.Changed().Throttle(TimeSpan.FromMilliseconds(1)).Subscribe(_ => {
						subscription.Dispose();
						w.Dispatcher.Invoke(w.Close);
					});
					var keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice,
						PresentationSource.FromDependencyObject(grid),
						0,
						Key.Enter);
					keyEventArgs.RoutedEvent = DataGrid.KeyDownEvent;
					grid.RaiseEvent(keyEventArgs);
				};
				w.Content = view;
			});
			Assert.IsInstanceOf<PriceOfferViewModel>(shell.ActiveItem);
		}
	}
}