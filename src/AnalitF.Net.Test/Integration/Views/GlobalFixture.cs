using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools.Calendar;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class GlobalFixture : BaseViewFixture
	{
		private Dispatcher dispatcher;
		private Window window;


		[TearDown]
		public void TearDown()
		{
			dispatcher.Invoke(() => window.Close());
			dispatcher.InvokeShutdown();
		}

		//тестируюет очень специфичная ошибка
		//проблема повторяется если открыть каталок -> войти в пребложения
		//вернуться в каталог "нажав букву" и если это повторная попытка поиска
		//и в предыдущую попытку был выбран элементы который отображается на одном экраные с выбранные
		//в текущую попытку элементом то это приведет к эффекту похожому на "съедание" ввыденной буквы
		[Test]
		public async void Open_catalog_on_quick_search()
		{
			var loaded = new SemaphoreSlim(0, 1);

			dispatcher = WpfHelper.WithDispatcher(() => {
				window = (Window)ViewLocator.LocateForModel(shell, null, null);
				window.Loaded += (sender, args) => loaded.Release();
				ViewModelBinder.Bind(shell, window, null);
				window.Show();
			});

			//открытие окна на весь экран нужно что бы отображалось максимальное колиечство элементов
			window.Dispatcher.Invoke(() => {
				window.WindowState = WindowState.Maximized;
			});
			await loaded.WaitAsync();
			shell.ShowCatalog();
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

		private void WaitIdle(Dispatcher dispatcher)
		{
			var wait = new ManualResetEventSlim();
			dispatcher.InvokeAsync(wait.Set, DispatcherPriority.ApplicationIdle);
			wait.Wait();
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
			var view = (FrameworkElement)viewModel.GetView();
			Input(view, "CatalogNames", Key.Enter);
			Input(view, "Catalogs", Key.Enter);
			var offers = (CatalogOfferViewModel)shell.ActiveItem;
			await ViewLoaded(offers);
			Thread.Sleep(1000);
			Input((FrameworkElement)offers.GetView(), "Offers", "б");
			await ViewLoaded<CatalogViewModel>(shell);

			WaitIdle(window.Dispatcher);
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

		private async Task<T> ViewLoaded<T>(ShellViewModel shell)
		{
			if (!SpinWait.SpinUntil(() => shell.ActiveItem is T, 10.Second()))
				throw new Exception(String.Format("Не удалось дождаться модели {0}", typeof(T)));

			await ViewLoaded((BaseScreen)shell.ActiveItem);
			return (T)shell.ActiveItem;
		}

		private Task ViewLoaded(BaseScreen viewModel)
		{
			var loaded = new SemaphoreSlim(0, 1);
			var view = viewModel.GetView();
			if (view != null)
				loaded.Release();
			else {
				EventHandler<ViewAttachedEventArgs> attached = null;
				attached = (sender, args) => {
					if (args.View is UserControl) {
						var userControl = ((UserControl)args.View);
						RoutedEventHandler loadedHandler = null;
						loadedHandler = (o, eventArgs) => {
							userControl.Loaded -= loadedHandler;
							viewModel.ViewAttached -= attached;
							loaded.Release();
						};
						userControl.Loaded += loadedHandler;
					}
				};
				viewModel.ViewAttached += attached;
			}
			return loaded.WaitAsync(5.Second());
		}
	}
}