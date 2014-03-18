using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using log4net.Config;
using Microsoft.Reactive.Testing;
using Microsoft.Win32;
using NHibernate.Linq;
using NHibernate.Util;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI.Testing;
using Action = System.Action;
using Address = AnalitF.Net.Client.Models.Address;
using CheckBox = System.Windows.Controls.CheckBox;
using DataGrid = System.Windows.Controls.DataGrid;
using DataGridCell = System.Windows.Controls.DataGridCell;
using Label = System.Windows.Controls.Label;
using Screen = Caliburn.Micro.Screen;
using TextBox = System.Windows.Controls.TextBox;
using WindowState = System.Windows.WindowState;
using WpfHelper = AnalitF.Net.Client.Helpers.WpfHelper;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class GlobalFixture : DispatcherFixture
	{
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
			activeWindow.Dispatcher.Invoke(() => {
				activeWindow.WindowState = WindowState.Maximized;
			});

			Click("ShowCatalog");
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
			var term = session.Query<CatalogName>()
				.First(n => n.HaveOffers && n.Name.StartsWith("б"))
				.Name.Slice(3).ToLower();

			Start();
			Click("ShowCatalog");

			var catalog = await ViewLoaded<CatalogViewModel>();
			dispatcher.Invoke(() => {
				catalog.CatalogSearch = true;
			});

			await ViewLoaded(catalog.ActiveItem);
			var search = (CatalogSearchViewModel)catalog.ActiveItem;
			var view = (FrameworkElement)search.GetView();
			Input(view, "SearchText", term);
			Input(view, "SearchText", Key.Enter);
			WaitIdle();

			dispatcher.Invoke(() => {
				var grid = (DataGrid)view.FindName("Items");
				var selectMany = grid.Descendants<DataGridCell>()
					.SelectMany(c => c.Descendants<Run>())
					.Where(r => r.Text.ToLower() == term);
				DoubleClick(view, grid, selectMany.First());
			});
			var offers = await ViewLoaded<CatalogOfferViewModel>();
			Assert.That(offers.Offers.Value.Count, Is.GreaterThan(0));
		}

		[Test]
		public async void Open_catalog()
		{
			session.DeleteEach<Order>();

			Start();

			Click("ShowCatalog");
			var catalog = await ViewLoaded<CatalogViewModel>();
			await ViewLoaded(catalog.ActiveItem);
			var name = (CatalogNameViewModel)catalog.ActiveItem;
			var offers = await OpenOffers(name);
			Input((FrameworkElement)offers.GetView(), "Offers", "1");

			Click("ShowOrderLines");
			var lines = (OrderLinesViewModel)shell.ActiveItem;
			await ViewLoaded(lines);
			Input((FrameworkElement)lines.GetView(), "Lines", Key.Enter);
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public async void Quick_search()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder(toAddress: session.Query<Address>().OrderBy(a => a.Name).First());
			var offer = session.Query<Offer>().First(o => o.ProductSynonym != order.Lines[0].ProductSynonym);
			order.TryOrder(offer, 1);
			var source = order.Lines.OrderBy(l => l.ProductSynonym).ToArray();
			var term = source[1].ProductSynonym.ToLower().Except(source[0].ProductSynonym.ToLower()).First().ToString();

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
				Assert.AreEqual(source[1].Id, ((OrderLine)grid.SelectedItem).Id,
					"term = {0}, value = {1}", term, grid.SelectedItem);
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
			Fixture<CreateAddress>();

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
			Fixture(new UnknownWaybill());

			Start();
			Click("ShowWaybills");
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

		[Test]
		public void Order_details()
		{
			restore = true;
			session.DeleteEach<Order>();
			var user = session.Query<User>().First();
			user.IsPreprocessOrders = true;
			Fixture<CorrectOrder>();

			Start();
			Click("ShowOrders");
			Input("Orders", Key.Enter);
			WaitIdle();

			dispatcher.Invoke(() => {
				var details = (OrderDetailsViewModel)shell.ActiveItem;
				var view = (FrameworkElement)details.GetView();
				var check = (CheckBox)view.FindName("OnlyWarning");
				Assert.IsTrue(check.IsVisible);

				var count = (Label)view.FindName("Source_Count");
				Assert.AreEqual(2, count.Content);
				Assert.False(check.IsChecked.Value);
				check.IsChecked = true;

				details.CurrentLine.Value = details.Lines.Value.First();

				var text = (TextBox)view.FindName("ErrorText");
				Assert.IsTrue(text.IsVisible);
				Assert.AreEqual("предложение отсутствует ", text.Text);
			});
		}

		[Test]
		public void Edit_order_count()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();
			Start();
			Click("ShowOrders");
			Input("Orders", Key.Enter);
			WaitIdle();

			Input("Lines", "2");

			dispatcher.Invoke(() => {
				var grid = activeWindow.Descendants<DataGrid>().First(g => g.Name == "Lines");
				var cell = GetCell(grid, "Заказ");
				Assert.AreEqual("2", ((TextBlock)cell.Content).Text);
			});
			Input("Lines", Key.Escape);

			session.Refresh(order);
			Assert.AreEqual(2, order.Lines[0].Count);
		}

		[Test]
		public void Make_order_correction()
		{
			MakeOrder();
			Fixture<RandCost>();

			Start();
			AsyncClick("Update");

			WaitMessageBox("Обновление завершено успешно.");
			WaitWindow("Корректировка восстановленных заказов");
		}

		[Test]
		public void Open_prices()
		{
			Start();

			Click("ShowPrice");
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)((PriceViewModel)shell.ActiveItem).GetView();
				var block = (TextBlock)view.FindName("CurrentPrice_Value_SupplierFullName");
				Assert.That(block.Text, Is.StringContaining("Тестовый"));
				var text = (TextBox)view.FindName("CurrentPrice_Value_ContactInfo");
				Assert.That(text.Text, Is.StringContaining("тестовая контактная информация"));
			});
		}

		[Test]
		public void Offers_search()
		{
			//нам нужно лубое наименование где есть хотя бы 3 буквы
			//тк цифры будут считаться вводом для редактирования
			var term = session.Query<Offer>().Take(100).ToArray().Select(o => Regex.Match(o.ProductSynonym, "[a-zA-Zа-яА-Я]{3}"))
				.Where(m => m.Success)
				.Select(m => m.Captures[0].Value)
				.First();
			Start();
			Click("SearchOffers");

			var search = (SearchOfferViewModel)shell.ActiveItem;
			var view = (FrameworkElement)search.GetView();

			Input(view, "Offers", term);
			Input(view, "Offers", Key.Enter);
			dispatcher.Invoke(() => {
				var offers = (DataGrid)view.FindName("Offers");
				Assert.That(offers.Items.Count, Is.GreaterThan(0), "Поисковый запрос '{0}'", term);
			});
		}

		[Test]
		public void Load_order_history()
		{
			var order = MakeSentOrder();
			var catalog = session.Load<Catalog>(order.Lines[0].CatalogId);

			Start();
			Click("ShowCatalog");
			var catalogModel = (CatalogViewModel)shell.ActiveItem;
			var viewModel = (CatalogNameViewModel)catalogModel.ActiveItem;
			var view = (FrameworkElement)viewModel.GetView();
			dispatcher.Invoke(() => {
				var names = (DataGrid)view.FindName("CatalogNames");
				names.SelectedItem = names.ItemsSource.Cast<CatalogName>().First(n => n.Id == catalog.Name.Id);
				var catalogs = (DataGrid)view.FindName("Catalogs");
				catalogs.SelectedItem = catalogs.ItemsSource.Cast<Catalog>().First(n => n.Id == catalog.Id);
			});
			Input(view, "CatalogNames", Key.Enter);
			if (viewModel.Catalogs.Value.Count > 1)
				Input(view, "Catalogs", Key.Enter);
			dispatcher.Invoke(() => {
				testScheduler.AdvanceByMs(3000);
			});
			WaitIdle();
			dispatcher.Invoke(() => {
				var element = (FrameworkElement)((Screen)shell.ActiveItem).GetView();
				var grid = (DataGrid)element.FindName("HistoryOrders");
				Assert.That(grid.Items.Count, Is.GreaterThan(0));
			});
		}

		[Test]
		public void Dynamic_recalculate_markup_validation()
		{
			Start();
			AsyncClick("ShowSettings");
			dispatcher.Invoke(() => {
				var content = (FrameworkElement)activeWindow.Content;
				var tab = (TabItem)content.FindName("VitallyImportantMarkupsTab");
				tab.IsSelected = true;
			});
			WaitIdle();
			dispatcher.Invoke(() => {
				var content = (FrameworkElement)activeWindow.Content;
				var grid = (DataGrid)content.FindName("VitallyImportantMarkups");
				EditCell(grid, 0, 1, "30");
				Assert.AreEqual(Color.FromRgb(0x80, 0x80, 0).ToString(), GetCell(grid, 0, 1).Background.ToString());
			});
		}

		[Test]
		public void Update_catalog_info()
		{
			Start();
			Click("ShowCatalog");

			var catalogModel = (CatalogViewModel)shell.ActiveItem;
			var viewModel = (CatalogNameViewModel)catalogModel.ActiveItem;
			var view = (FrameworkElement)viewModel.GetView();
			dispatcher.Invoke(() => {
				var names = (DataGrid)view.FindName("CatalogNames");
				var current = (CatalogName)names.SelectedItem;
				var toSelect = names.ItemsSource.Cast<CatalogName>().First(n => n.Id != current.Id && n.Mnn != null);
				names.SelectedItem = toSelect;

				var mnn = view.Descendants<Label>().First(l => l.Name == "CurrentCatalogName_Mnn_Name");
				Assert.AreEqual(toSelect.Mnn.Name, mnn.Content);
			});
		}

		[Test]
		public void Warn_on_waybill_reject()
		{
			Fixture<RejectedWaybill>();
			Start();
			AsyncClick("Update");

			WaitWindow("АналитФАРМАЦИЯ: Внимание");
			dispatcher.Invoke(() => {
				Assert.That(activeWindow.AsText(),
					Is.StringContaining("Обнаружены препараты," +
						" предписанные к изъятию, в имеющихся у Вас электронных накладных."));
			});
			Click("Show");

			var model = (WaybillsViewModel)shell.ActiveItem;
			var view = (FrameworkElement)model.GetView();
			dispatcher.Invoke(() => {
				var waybills = (DataGrid)view.FindName("Waybills");
				Assert.AreEqual(1, waybills.Items.Count);
			});
		}

		[Test]
		public void Delay_of_payment()
		{
			Fixture<LocalDelayOfPayment>();
			Start();
			Click("ShowCatalog");
			OpenOffers();

			dispatcher.Invoke(() => {
				var offers = activeWindow.Descendants<DataGrid>().First(g => g.Name == "Offers");

				var supplierCost = GetCell(offers, "Цена поставщика");
				var cost = GetCell(offers, "Цена");
				Assert.AreNotEqual(supplierCost.AsText(), cost.AsText());
			});
		}

		[Test]
		public void Promotion()
		{
			session.DeleteEach<Promotion>();
			var fixture = new LocalPromotion("assets/Валемидин.JPG");
			Fixture(fixture);

			Start();
			Click("ShowCatalog");
			OpenOffers(fixture.Promotion.Catalogs[0]);
			dispatcher.Invoke(() => {
				var promotions = activeWindow.Descendants<Client.Views.Parts.PromotionPopup>().First();
				Assert.IsTrue(promotions.IsVisible);
				Assert.That(promotions.AsText(), Is.StringContaining(fixture.Promotion.Name));
				var presenter = promotions.Descendants<ContentPresenter>()
					.First(c => c.DataContext is Promotion && ((Promotion)c.DataContext).Id == fixture.Promotion.Id);
				var link = presenter.Descendants<TextBlock>().SelectMany(b => b.Inlines).OfType<Hyperlink>().First();
				dispatcher.BeginInvoke(new Action(() => InternalClick(link)));
			});

			WaitWindow(fixture.Promotion.DisplayName);
			dispatcher.Invoke(() => {
				var viewer = activeWindow.Descendants<FlowDocumentScrollViewer>().First();
				var image = viewer.Document.Descendants<Image>().First();
				Assert.IsNotNull(image);
				Assert.That(image.Source.Height, Is.GreaterThan(0));
			});
		}

		[Test]
		public void Smart_order()
		{
			var fixture = new SmartOrder {
				ProductIds = new [] {
					session.Query<Offer>().First().ProductId
				}
			};
			Fixture(fixture);
			var filename = TempFile("batch.txt", "1|10");
			session.DeleteEach<Order>();

			Start();
			Click("ShowBatch");

			manager.FileDialog.OfType<OpenFileDialog>().Take(1)
				.Subscribe(d => d.FileName = Path.GetFullPath(filename));
			AsyncClickNoWait("Upload");

			WaitWindow("Обмен данными");
			WaitMessageBox("Обновление завершено успешно.");
			WaitIdle();

			dispatcher.Invoke(() => {
				var items = activeWindow.Descendants<DataGrid>().First(g => g.Name == "ReportLines");
				Assert.That(items.Items.Count, Is.GreaterThan(0));
			});
		}

		private void WaitWindow(string title)
		{
			var opened = manager.WindowOpened.Timeout(30.Second()).First();
			opened.Dispatcher.Invoke(() => {
				Assert.AreEqual(title, opened.Title, opened.AsText());
			});
		}

		private void WaitMessageBox(string message)
		{
			var opened = manager.MessageOpened.Timeout(15.Second()).First();
			Assert.AreEqual(opened, message);
			var window = WinApi.FindWindow(IntPtr.Zero, "АналитФАРМАЦИЯ: Информация");
			for(var i = 0; window == IntPtr.Zero && i < 100; i++) {
				Thread.Sleep(20);
				window = WinApi.FindWindow(IntPtr.Zero, "АналитФАРМАЦИЯ: Информация");
			}
			if (window == IntPtr.Zero)
				throw new Exception(String.Format("Не удалось найти окно '{0}'", "АналитФАРМАЦИЯ: Информация"));
			WinApi.SendMessage(window, WinApi.WM_CLOSE, 0, IntPtr.Zero);
		}

		private void EditCell(DataGrid grid, int column, int row, string text)
		{
			var cell = GetCell(grid, column, row);
			cell.Focus();
			Input(cell, Key.F2);
			var edit = cell.Descendants<TextBox>().First();
			Input(edit, text);
			Input(cell, Key.Enter);
		}

		private DataGridCell GetCell(DataGrid grid, string name, int row = 0)
		{
			var column = grid.Columns.First(c => Equals(c.Header, name));
			return GetCell(grid, column.DisplayIndex, row);
		}

		private DataGridCell GetCell(DataGrid grid, int column, int row)
		{
			var gridRow = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(row);
			var presenter = gridRow.VisualChild<DataGridCellsPresenter>();
			return (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
		}

		private static T Find<T>(FrameworkElement view, string root, string name) where T : FrameworkElement
		{
			return view.Descendants<FrameworkElement>()
				.First(e => e.Name == root)
				.Descendants<T>()
				.First(e => e.Name == name);
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
		}

		private CatalogOfferViewModel OpenOffers(Catalog catalog = null)
		{
			return OpenOffers((CatalogNameViewModel)((CatalogViewModel)shell.ActiveItem).ActiveItem, catalog).Result;
		}

		private async Task<CatalogOfferViewModel> OpenOffers(CatalogNameViewModel viewModel, Catalog catalog = null)
		{
			var view = (FrameworkElement)viewModel.GetView();
			if (view == null)
				throw new Exception(String.Format("Не удалось получить view из {0}", viewModel.GetType()));
			if (catalog != null) {
				dispatcher.Invoke(() => {
					var names = activeWindow.Descendants<DataGrid>().First(g => g.Name == "CatalogNames");
					names.SelectedItem = names.ItemsSource.Cast<CatalogName>().First(c => c.Id == catalog.Name.Id);
				});
			}
			Input(view, "CatalogNames", Key.Enter);
			if (viewModel.Catalogs.Value.Count > 1) {
				if (catalog != null) {
					dispatcher.Invoke(() => {
						var catalogs = activeWindow.Descendants<DataGrid>().First(g => g.Name == "Catalogs");
						catalogs.SelectedItem = catalogs.ItemsSource.Cast<Catalog>().First(c => c.Id == catalog.Id);
					});
				}
				Input(view, "Catalogs", Key.Enter);
			}
			var offers = (CatalogOfferViewModel)shell.ActiveItem;
			await ViewLoaded(offers);
			WaitIdle();
			return offers;
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
