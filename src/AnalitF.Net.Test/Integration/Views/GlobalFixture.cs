using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using log4net.Config;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class GlobalFixture : BaseViewFixture
	{
		private Dispatcher dispatcher;
		private Window window;

		[SetUp]
		public void Setup()
		{
			shell.Quiet = true;
			shell.ViewModelSettings.Clear();
		}

		[TearDown]
		public void TearDown()
		{
			dispatcher.Invoke(() => window.Close());
			dispatcher.InvokeShutdown();
		}

		//тестирует очень специфичную ошибку
		//проблема повторяется если открыть каталок -> войти в пребложения
		//вернуться в каталог "нажав букву" и если это повторная попытка поиска
		//и в предыдущую попытку был выбран элементы который отображается на одном экраные с выбранные
		//в текущую попытку элементом то это приведет к эффекту похожому на "съедание" ввыденной буквы
		[Test]
		public async void Open_catalog_on_quick_search()
		{
			Start();

			//открытие окна на весь экран нужно что бы отображалось максимальное колиечство элементов
			window.Dispatcher.Invoke(() => {
				window.WindowState = WindowState.Maximized;
			});

			dispatcher.Invoke(() => shell.ShowCatalog());
			var catalog = (CatalogViewModel)shell.ActiveItem;
			await ViewLoaded(catalog);
			var names = (CatalogNameViewModel)catalog.ActiveItem;
			var name = names.CurrentCatalogName.Value.Name;
			WaitIdle();

			var term = "б";
			//нужно сделать две итерации тк что бы FocusBehavior попытался восстановить выбранный элемент
			await OpenAndReturnOnSearch(names, term);
			AssertQuickSearch(catalog, term);

			names = (CatalogNameViewModel)catalog.ActiveItem;
			var frameworkElement = (FrameworkElement)names.GetView();
			Input(frameworkElement, "CatalogNames", Key.Escape);
			frameworkElement.Dispatcher.Invoke(() => {
				names.CurrentCatalogName.Value = names.CatalogNames.Value.First(n => n.Name == name);
			});
			await OpenAndReturnOnSearch(names, term);
			AssertQuickSearch(catalog, term);
		}

		[Test]
		public async void Open_catalog_offers()
		{
			Start();

			dispatcher.Invoke(() => {
				shell.ShowCatalog();
			});
			WaitIdle();
			var catalog = await ViewLoaded<CatalogViewModel>();
			dispatcher.Invoke(() => {
				catalog.CatalogSearch = true;
			});

			await ViewLoaded(catalog.ActiveItem);
			var search = (CatalogSearchViewModel)catalog.ActiveItem;
			var view = (FrameworkElement)search.GetView();
			Input(view, "SearchText", "бак");
			Input(view, "SearchText", Key.Enter);
			WaitIdle();

			dispatcher.Invoke(() => {
				var grid = (DataGrid)view.FindName("Items");
				var selectMany = grid.Descendants<DataGridCell>()
					.SelectMany(c => XamlExtentions.Descendants<Run>(c))
					.Where(r => r.Text.ToLower() == "бак");
				DoubleClick(view, grid, selectMany.First());
			});
			var offers = await ViewLoaded<CatalogOfferViewModel>();
			Assert.That(offers.Offers.Count, Is.GreaterThan(0));
		}

		[Test]
		public async void Open_catalog()
		{
			session.DeleteEach<Order>();
			session.Flush();

			Start();

			dispatcher.Invoke(() => {
				shell.ShowCatalog();
			});

			var catalog = await ViewLoaded<CatalogViewModel>();
			await ViewLoaded(catalog.ActiveItem);
			var name = (CatalogNameViewModel)catalog.ActiveItem;
			var offers = await OpenOffers(name);
			WaitIdle();
			Input((FrameworkElement)offers.GetView(), "Offers", "1");
			dispatcher.Invoke(() => {
				shell.ShowOrderLines();
			});
			var lines = (OrderLinesViewModel)shell.ActiveItem;
			await ViewLoaded(lines);
			Input((FrameworkElement)lines.GetView(), "Lines", Key.Enter);
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public async void Quick_search()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder(toAddress: session.Query<Client.Models.Address>().OrderBy(a => a.Name).First());
			var offer = session.Query<Offer>().First(o => o.ProductSynonym != order.Lines[0].ProductSynonym);
			order.AddLine(offer, 1);
			var source = order.Lines.OrderBy(l => l.ProductSynonym).ToArray();
			var term = source[1].ProductSynonym.Except(source[0].ProductSynonym).First().ToString();
			session.Flush();

			Start();
			Click("ShowOrderLines");
			var lines = await ViewLoaded<OrderLinesViewModel>();
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)lines.GetView();
				var grid = (DataGrid)view.FindName("Lines");
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				Assert.AreEqual(source[0].Id, ((OrderLine)grid.SelectedItem).Id);
				Assert.AreEqual(source[0].Id, ((OrderLine)grid.CurrentItem).Id);

				Input(grid, term);
				Assert.AreEqual(source[1].Id, ((OrderLine)grid.SelectedItem).Id);
				Assert.AreEqual(source[1].Id, ((OrderLine)grid.CurrentItem).Id);
				var text = Find<TextBox>(view, "QuickSearch", "SearchText");
				Assert.True(text.IsVisible);
				Assert.AreEqual(term, text.Text);
			});
		}

		[Test]
		public async void Current_address_visivility()
		{
			restore = true;
			new CreateAddress().Execute(session);

			Start();
			Click("ShowOrderLines");
			var lines = await ViewLoaded<OrderLinesViewModel>();
			Assert.IsFalse(lines.AddressSelector.All.Value);
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)lines.GetView();
				var box = Find<CheckBox>(view, "AddressSelector", "All");
				Assert.IsFalse(box.IsChecked.Value);

				var grid = (DataGrid)view.FindName("Lines");
				var column = grid.Columns.First(c => "Адрес заказа".Equals(c.Header));
				Assert.AreEqual(Visibility.Collapsed, column.Visibility);
			});
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)lines.GetView();
				var box = Find<CheckBox>(view, "AddressSelector", "All");
				box.IsChecked = true;

				var grid = (DataGrid)view.FindName("Lines");
				var column = grid.Columns.First(c => "Адрес заказа".Equals(c.Header));
				Assert.IsTrue(box.IsChecked.Value);
				Assert.IsTrue(lines.AddressSelector.All.Value);
				Assert.AreEqual(Visibility.Visible, column.Visibility);

				box.IsChecked = false;
			});
			Click("ShowOrders");
			WaitIdle();
			Click("ShowOrderLines");
			lines = await ViewLoaded<OrderLinesViewModel>();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)lines.GetView();
				var box = Find<CheckBox>(view, "AddressSelector", "All");
				Assert.IsFalse(box.IsChecked.Value);

				var grid = (DataGrid)view.FindName("Lines");
				var column = grid.Columns.First(c => "Адрес заказа".Equals(c.Header));
				Assert.AreEqual(Visibility.Collapsed, column.Visibility);
			});
		}

		[Test]
		public void Select_printing_by_header()
		{
			new UnknownWaybill().Execute(session);

			Start();
			Click("ShowWaybills");
			WaitIdle();
			Input("Waybills", Key.Enter);
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)((WaybillDetails)shell.ActiveItem).GetView();
				var datagrid = (DataGrid)view.FindName("Lines");
				var printColumn = datagrid.Columns.First(c => !(c.Header is String));
				var all = datagrid.Descendants<CheckBox>().First(c => "Печатать".Equals(c.Content));
				Assert.IsTrue(all.IsChecked.Value);
				all.IsChecked = false;

				datagrid.Descendants<DataGridCell>()
					.Where(c => c.Column == printColumn)
					.SelectMany(c => c.Descendants<CheckBox>())
					.Each(c => Assert.IsFalse(c.IsChecked.Value));
			});
		}

		private static T Find<T>(FrameworkElement view, string root, string name) where T : FrameworkElement
		{
			return view.Descendants<FrameworkElement>()
				.First(e => e.Name == root)
				.Descendants<T>()
				.First(e => e.Name == name);
		}

		private async void Start()
		{
			var loaded = new SemaphoreSlim(0, 1);

			dispatcher = WpfHelper.WithDispatcher(() => {
				window = (Window)ViewLocator.LocateForModel(shell, null, null);
				window.Loaded += (sender, args) => loaded.Release();
				ViewModelBinder.Bind(shell, window, null);
				window.Show();
			});

			await loaded.WaitAsync();
			WaitIdle();
		}

		private void WaitIdle()
		{
			dispatcher.Invoke(() => {}, DispatcherPriority.ContextIdle);
		}

		private void AssertQuickSearch(CatalogViewModel catalog, string term)
		{
			var view = (FrameworkElement)catalog.ActiveItem.GetView();
			view.Dispatcher.Invoke(() => {
				var text = Find<TextBox>(view, "CatalogNamesSearch", "SearchText");
				Assert.AreEqual(Visibility.Visible, text.Visibility);
				Assert.AreEqual(term, text.Text);
				var names = (CatalogNameViewModel)catalog.ActiveItem;
				var name = names.CatalogNames.Value.First(n => n.Name.ToLower().StartsWith(term));
				var grid = (DataGrid)view.FindName("CatalogNames");
				Assert.AreEqual(name, grid.SelectedItem);
				Assert.AreEqual(name, grid.CurrentItem);
			});

			//после активации формы нужно подождать что бы произошли фоновые дела
			//layout, redner и тд если этого не делать код будет работать не верно
			WaitIdle();
		}

		private async Task OpenAndReturnOnSearch(CatalogNameViewModel viewModel, string term)
		{
			var offers = await OpenOffers(viewModel);
			Input((FrameworkElement)offers.GetView(), "Offers", term);
			await ViewLoaded<CatalogViewModel>();

			WaitIdle();
		}

		private async Task<CatalogOfferViewModel> OpenOffers(CatalogNameViewModel viewModel)
		{
			var view = (FrameworkElement)viewModel.GetView();
			if (view == null)
				throw new Exception(String.Format("Не удалось получить view из {0}", viewModel.GetType()));
			Input(view, "CatalogNames", Key.Enter);
			if (viewModel.Catalogs.Value.Count > 1)
				Input(view, "Catalogs", Key.Enter);
			var offers = (CatalogOfferViewModel)shell.ActiveItem;
			await ViewLoaded(offers);
			return offers;
		}

		public void DoubleClick(FrameworkElement view, UIElement element, object origin = null)
		{
			element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) {
				RoutedEvent = Control.MouseDoubleClickEvent,
				Source = origin,
			});
		}

		public void Click(ButtonBase element)
		{
			Contract.Assert(element != null);
			AssertInputable(element);
			element.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, element));
		}

		public void Click(string name)
		{
			dispatcher.Invoke(() => {
				Click((ButtonBase)window.FindName(name));
			});
		}

		private void Input(FrameworkElement view, string name, string text)
		{
			view.Dispatcher.Invoke(() => {
				Input((UIElement)view.FindName(name), text);
			});
		}

		private void Input(UIElement element, string text)
		{
			Contract.Assert(element != null);
			AssertInputable(element);
			element.RaiseEvent(WpfHelper.TextArgs(text));
		}

		private void Input(string name, Key key)
		{
			var item = (BaseScreen)shell.ActiveItem;
			var view = item.GetView();
			Contract.Assert(view != null);
			Input((FrameworkElement)view, name, key);
		}

		private void Input(FrameworkElement view, string name, Key key)
		{
			view.Dispatcher.Invoke(() => {
				var element = (UIElement)view.FindName(name);
				if (element == null)
					throw new Exception(String.Format("Не могу найти {0}", name));
				AssertInputable(element);
				element.RaiseEvent(WpfHelper.KeyEventArgs(element, key));
			});
		}

		private static void AssertInputable(UIElement element)
		{
			Assert.IsTrue(element.IsVisible);
			Assert.IsTrue(element.IsEnabled);
		}

		private async Task<T> ViewLoaded<T>()
		{
			if (!SpinWait.SpinUntil(() => shell.ActiveItem is T, 10.Second()))
				throw new Exception(String.Format("Не удалось дождаться модели {0} текущая модель {1}", typeof(T),
					shell.ActiveItem == null ? "null" : shell.ActiveItem.GetType().ToString()));

			await ViewLoaded((BaseScreen)shell.ActiveItem);
			return (T)shell.ActiveItem;
		}

		private async Task ViewLoaded(Screen viewModel)
		{
			var view = viewModel.GetView() as FrameworkElement;
			if (view == null) {
				await Observable.FromEventPattern(viewModel, "ViewAttached")
					.Take(1)
					.ToTask();
				view = viewModel.GetView() as FrameworkElement;
			}

			IObservable<EventPattern<EventArgs>> wait = null;
			dispatcher.Invoke(() => {
				if (!view.IsLoaded) {
					wait = Observable.FromEventPattern(view, "Loaded").Take(1);
					//todo: не понятно почему но если не делать здесь subscribe, wait.ToTask()
					//не завершится, может событие происходит до того как мы успеваем сделать ToTask
					//но это происходит стабильно и почему помогает Subscribe?
					wait.Subscribe(_ => {});
				}
				else {
					wait = Observable.Return(new EventPattern<EventArgs>(null, null));
				}
			});
			await wait.ToTask();
		}
	}
}