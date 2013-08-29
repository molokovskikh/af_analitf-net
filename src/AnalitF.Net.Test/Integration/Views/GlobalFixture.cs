using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools.Calendar;
using log4net.Config;
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
			var name = names.CurrentCatalogName.Name;
			WaitIdle(dispatcher);

			//нужно сделать две итерации тк что бы FocusBehavior попытался восстановить выбранный элемент
			await OpenAndReturnOnSearch(window, names);
			AssertQuickSearch(dispatcher, catalog);

			names = (CatalogNameViewModel)catalog.ActiveItem;
			var frameworkElement = (FrameworkElement)names.GetView();
			Input(frameworkElement, "CatalogNames", Key.Escape);
			frameworkElement.Dispatcher.Invoke(() => {
				names.CurrentCatalogName = names.CatalogNames.First(n => n.Name == name);
			});
			await OpenAndReturnOnSearch(window, names);
			AssertQuickSearch(dispatcher, catalog);
		}

		[Test]
		public async void Open_catalog_offers()
		{
			Start();

			dispatcher.Invoke(() => {
				shell.ShowCatalog();
			});
			WaitIdle(dispatcher);
			var catalog = await ViewLoaded<CatalogViewModel>();
			dispatcher.Invoke(() => {
				catalog.CatalogSearch = true;
			});

			await ViewLoaded(catalog.ActiveItem);
			var search = (CatalogSearchViewModel)catalog.ActiveItem;
			var view = (FrameworkElement)search.GetView();
			Input(view, "SearchText", "бак");
			Input(view, "SearchText", Key.Enter);
			WaitIdle(dispatcher);

			dispatcher.Invoke(() => {
				var grid = (DataGrid)view.FindName("Catalogs");
				var selectMany = grid.DeepChildren()
					.OfType<DataGridCell>().SelectMany(c => c.DeepChildren().OfType<Run>())
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
			WaitIdle(dispatcher);
			Input((FrameworkElement)offers.GetView(), "Offers", "1");
			dispatcher.Invoke(() => {
				shell.ShowOrderLines();
			});
			var lines = (OrderLinesViewModel)shell.ActiveItem;
			await ViewLoaded(lines);
			Input((FrameworkElement)lines.GetView(), "Lines", Key.Enter);
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
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
			WaitIdle(dispatcher);
		}

		private void WaitIdle(Dispatcher dispatcher)
		{
			dispatcher.Invoke(() => {}, DispatcherPriority.ContextIdle);
		}

		private void AssertQuickSearch(Dispatcher d, CatalogViewModel catalog)
		{
			var view = (FrameworkElement)catalog.ActiveItem.GetView();
			view.Dispatcher.Invoke(() => {
				var text = (TextBox)view.FindName("CatalogNamesSearch_SearchText");
				Assert.AreEqual(Visibility.Visible, text.Visibility);
				Assert.AreEqual("б", text.Text);
				var names = (CatalogNameViewModel)catalog.ActiveItem;
				var name = names.CatalogNames.First(n => n.Name.ToLower().StartsWith("б"));
				var grid = (DataGrid)view.FindName("CatalogNames");
				Assert.AreEqual(name, grid.SelectedItem);
				Assert.AreEqual(name, grid.CurrentItem);
			});

			//после активации формы нужно подождать что бы произошли фоновые дела
			//layout, redner и тд если этого не делать код будет работать не верно
			WaitIdle(d);
		}

		private async Task OpenAndReturnOnSearch(Window window, CatalogNameViewModel viewModel)
		{
			var offers = await OpenOffers(viewModel);
			Input((FrameworkElement)offers.GetView(), "Offers", "б");
			await ViewLoaded<CatalogViewModel>();

			WaitIdle(window.Dispatcher);
		}

		private async Task<CatalogOfferViewModel> OpenOffers(CatalogNameViewModel viewModel)
		{
			var view = (FrameworkElement)viewModel.GetView();
			if (view == null)
				throw new Exception(String.Format("Не удалось получить view из {0}", viewModel.GetType()));
			Input(view, "CatalogNames", Key.Enter);
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

		private void Input(FrameworkElement view, string name, string text)
		{
			view.Dispatcher.Invoke(() => {
				var element = (UIElement)view.FindName(name);
				if (element == null)
					throw new Exception(String.Format("Не могу найти {0}", name));
				element.RaiseEvent(WpfHelper.TextArgs(text));
			});
		}

		private void Input(FrameworkElement view, string name, Key key)
		{
			view.Dispatcher.Invoke(() => {
				var element = (UIElement)view.FindName(name);
				if (element == null)
					throw new Exception(String.Format("Не могу найти {0}", name));
				element.RaiseEvent(WpfHelper.KeyEventArgs(element, key));
			});
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