using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Client.Views.Parts;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using Action = System.Action;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using CheckBox = System.Windows.Controls.CheckBox;
using DataGrid = System.Windows.Controls.DataGrid;
using DataGridCell = System.Windows.Controls.DataGridCell;
using Label = System.Windows.Controls.Label;
using Menu = System.Windows.Controls.Menu;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Panel = System.Windows.Controls.Panel;
using Screen = Caliburn.Micro.Screen;
using TextBox = System.Windows.Controls.TextBox;
using AnalitFContlos = AnalitF.Net.Client.Controls;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class GlobalFixture : DispatcherFixture
	{
		//тестирует очень специфичную ошибку
		//проблема повторяется если открыть каталог -> войти в предложения
		//вернуться в каталог "нажав букву" и если это повторная попытка поиска
		//и в предыдущую попытку был выбран элементы который отображается на одном экране с выбранным
		//в текущую попытку элементом то это приведет к эффекту похожему на "съедание" введенной буквы
		[Test]
		public async Task Open_catalog_on_quick_search()
		{
			StartWait();

			//открытие окна на весь экран нужно что бы отображалось максимальное количество элементов
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
		public async Task Open_catalog_offers()
		{
			var term = session.Query<CatalogName>()
				.First(n => n.HaveOffers && n.Name.StartsWith("б"))
				.Name.Slice(3).ToLower();

			StartWait();
			Click("ShowCatalog");

			var catalog = await ViewLoaded<CatalogViewModel>();
			dispatcher.Invoke(() =>	{
				catalog.CatalogSearch.Value = true;
			});
			WaitIdle();
			await ViewLoaded(catalog.ActiveItem);
			var search = (CatalogSearchViewModel)catalog.ActiveItem;
			var view = (FrameworkElement)search.GetView();
			Input(view, "SearchText", term);
			Input(view, "SearchText", Key.Enter);

			dispatcher.Invoke(() =>	{
				scheduler.AdvanceByMs(100);
			});
			catalog.WaitQueryDrain().Wait();

			WaitIdle();

			dispatcher.Invoke(() =>	{
				var grid = (DataGrid)view.FindName("Items");
				var selectMany = grid.Descendants<DataGridCell>()
					.SelectMany(c => c.Descendants<Run>())
					.Where(r => r.Text.ToLower().Contains(term));
				var text = selectMany.FirstOrDefault();
				Assert.IsNotNull(text, "Не удалось найти ни одного элемента с текстом {0}, всего элементов {1}",
					term, grid.Items.Count);
				DoubleClick(grid, text);
			});
			var offers = await ViewLoaded<CatalogOfferViewModel>();
			Assert.That(offers.Offers.Value.Count, Is.GreaterThan(0));
		}

		[Test]
		public async Task Open_catalog()
		{
			session.DeleteEach<Order>();

			StartWait();

			Click("ShowCatalog");
			var catalog = await ViewLoaded<CatalogViewModel>();
			await ViewLoaded(catalog.ActiveItem);
			var name = (CatalogNameViewModel)catalog.ActiveItem;
			var load = session.Load<Catalog>(session.Query<Offer>().First(x => x.RequestRatio == null).CatalogId);
			var offers = await OpenOffers(name, load);
			Input((FrameworkElement)offers.GetView(), "Offers", "1");

			Click("ShowOrderLines");
			var lines = (OrderLinesViewModel)shell.ActiveItem;
			await ViewLoaded(lines);
			AdvanceScheduler(500);
			Assert.IsTrue(lines.ProductInfo.CanShowCatalog);
			Input((FrameworkElement)lines.GetView(), "Lines", Key.F2);
			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public async Task Quick_search()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder(toAddress: session.Query<Address>().OrderBy(a => a.Name).First());
			var offer = session.Query<Offer>().First(o => o.ProductSynonym != order.Lines[0].ProductSynonym);
			order.TryOrder(offer, 1);
			var source = order.Lines.OrderBy(l => l.ProductSynonym).ToArray();
			var term = source[1].ProductSynonym.ToLower().Except(source[0].ProductSynonym.ToLower()).First().ToString();

			StartWait();
			Click("ShowOrderLines");
			var lines = await ViewLoaded<OrderLinesViewModel>();
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)lines.GetView();
				var grid = (DataGrid)view.FindName("Lines");
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				grid.SelectedItem = grid.Items.OfType<OrderLine>().First(x => x.Id == source[0].Id);
				grid.CurrentItem = grid.Items.OfType<OrderLine>().First(x => x.Id == source[0].Id);
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
		public async Task Current_address_visivility()
		{
			restore = true;
			Fixture<LocalAddress>();

			StartWait();
			Click("ShowOrderLines");
			var lines = await ViewLoaded<OrderLinesViewModel>();
			Assert.IsFalse(lines.AddressSelector.All.Value);
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)lines.GetView();
				var box = Find<CheckBox>(view, "AddressSelector", "All");
				Assert.IsFalse(box.IsChecked.Value);

				var grid = (DataGrid)view.FindName("Lines");
				var column = DataGridHelper.FindColumn(grid.Columns, "Адрес заказа");
				Assert.AreEqual(Visibility.Collapsed, column.Visibility);
			});
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)lines.GetView();
				var box = Find<CheckBox>(view, "AddressSelector", "All");
				box.IsChecked = true;

				var grid = (DataGrid)view.FindName("Lines");
				var column = DataGridHelper.FindColumn(grid, "Адрес заказа");
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
				var column = DataGridHelper.FindColumn(grid, "Адрес заказа");
				Assert.AreEqual(Visibility.Collapsed, column.Visibility);
			});
		}

		[Test]
		public void Select_printing_by_header()
		{
			Fixture(new LocalWaybill());

			StartWait();
			Click("ShowWaybills");
			dispatcher.Invoke(() => {
				scheduler.AdvanceByMs(100);
			});
			WaitIdle();
			Input("Waybills", Key.Enter);
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)((WaybillDetails)shell.ActiveItem).GetView();
				var datagrid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
				var printColumn = datagrid.Columns.First(c => !(c.Header is TextBlock));
				var all = datagrid.Descendants<CheckBox>().First(x => x.Name == "CheckAllPrint");
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

			StartWait();
			Click("ShowOrders");
			Input("Orders", Key.Enter);
			WaitIdle();

			dispatcher.Invoke(() => {
				var details = (OrderDetailsViewModel)shell.ActiveItem;
				var view = (FrameworkElement)details.GetView();
				var count = (Label)view.FindName("Source_Count");
				Assert.AreEqual(2, count.Content);

				details.FilterItems.Where(x => x.Item.Item1 != "OnlyWarning").Each(x => x.IsSelected = false);
				scheduler.AdvanceByMs(10000);
				Assert.AreEqual(1, details.Lines.Value.Count);

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
			StartWait();
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
			session.DeleteEach<Order>();
			MakeOrder();
			Fixture<RandCost>();

			StartWait();
			AsyncClick("Update");

			WaitMessageBox("Обновление завершено успешно.");
			WaitWindow("Корректировка восстановленных заказов");
			var line = session.Query<OrderLine>().FirstOrDefault(l => l.SendResult == LineResultStatus.CostChanged);
			Assert.IsNotNull(session.Query<OrderLine>().ToArray().Implode(x => $"{x:r}"));
			dispatcher.Invoke(() => {
				var lines = activeWindow.Descendants<DataGrid>().First(x => x.Name == "Lines");
				var cells = lines.Descendants<DataGridCell>().Where(x => ((OrderLine)x.DataContext).Id == line.Id).ToArray();
				var oldcost = cells.First(x => ((Binding)((DataGridBoundColumn)x.Column).Binding).Path.Path == "MixedOldCost");
				var newcost = cells.First(x => ((Binding)((DataGridBoundColumn)x.Column).Binding).Path.Path == "MixedNewCost");
				if (line.IsCostDecreased) {
					//цвет может быть смешанный если строка выбрана или не смешанный если строка не выбрана
					//#FFCBF6D5 - смешанный активный
					//#FFCDEAB9 - смешанный неактивный
					Assert.That(oldcost.Background.ToString(), Is.EqualTo("#FFCBF6D5").Or.EqualTo("#FFB8FF71").Or.EqualTo("#FFCDEAB9"));
					Assert.That(newcost.Background.ToString(), Is.EqualTo("#FFCBF6D5").Or.EqualTo("#FFB8FF71").Or.EqualTo("#FFCDEAB9"));
				}
				else {
					//цвет может быть смешанный если строка выбрана или не смешанный если строка не выбрана
					//#FFE1C5D6 - смешанный активный
					//#FFE3B4BA - смешанный неактивный
					Assert.That(oldcost.Background.ToString(), Is.EqualTo("#FFCDEAB9").Or.EqualTo("#FFE1C5D6").Or.EqualTo("#FFE3B4BA"));
					Assert.That(newcost.Background.ToString(), Is.EqualTo("#FFEF5275").Or.EqualTo("#FFE1C5D6").Or.EqualTo("#FFE3B4BA"));
				}
			});

			WaitIdle();
			ShallowBindingErrors();
		}

		[Test]
		public void Open_prices()
		{
			StartWait();

			Click("ShowPrice");
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)((PriceViewModel)shell.ActiveItem).GetView();
				var block = (TextBlock)view.FindName("CurrentPrice_Value_SupplierFullName");
				Assert.That(block.Text, Does.Contain("Тестовый"));
				var text = (TextBox)view.FindName("CurrentPrice_Value_ContactInfo");
				Assert.That(text.Text, Does.Contain("тестовая контактная информация"));
			});
		}

		[Test]
		public void Show_price_offers()
		{
			StartWait();
			Click("ShowPrice");
			uint expectedCount = 0;
			dispatcher.Invoke(() => {
				var prices = activeWindow.Descendants<DataGrid>().First(g => g.Name == "Prices");
				var selectedItem = prices.Items.OfType<Price>().First(p => p.PositionCount > 0);
				expectedCount = selectedItem.PositionCount;
				prices.SelectedItem = selectedItem;
			});
			Input("Prices", Key.Enter);
			WaitIdle();
			dispatcher.Invoke(() => scheduler.Start());
			WaitIdle();
			dispatcher.Invoke(() => {
				var count = activeWindow.Descendants<Label>().First(l => l.Name == "Offers_Count");
				Assert.AreEqual(expectedCount.ToString(), count.AsText());
			});
		}

		[Test]
		public void Offers_search()
		{
			//нам нужно любое наименование где есть хотя бы 3 буквы
			//тк цифры будут считаться вводом для редактирования
			var term = session.Query<Offer>().Take(100).ToArray().Select(o => Regex.Match(o.ProductSynonym, "[a-zA-Zа-яА-Я]{3}"))
				.Where(m => m.Success)
				.Select(m => m.Captures[0].Value)
				.First();
			StartWait();
			Click("SearchOffers");

			var search = (SearchOfferViewModel)shell.ActiveItem;
			var view = (FrameworkElement)search.GetView();

			Input(view, "Offers", term);
			Input(view, "Offers", Key.Enter);
			WaitIdle();
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
			DebugContext.Add("CatalogId", catalog.Id);

			StartWait();
			Click("ShowCatalog");
			var catalogModel = (CatalogViewModel)shell.ActiveItem;
			var viewModel = (CatalogNameViewModel)catalogModel.ActiveItem;
			var view = (FrameworkElement)viewModel.GetView();
			dispatcher.Invoke(() =>	{
				var names = (DataGrid)view.FindName("CatalogNames");
				names.SelectedItem = names.ItemsSource.Cast<CatalogName>().First(n => n.Id == catalog.Name.Id);
			});
			WaitIdle();
			dispatcher.Invoke(() =>	{
				var catalogs = (DataGrid)view.FindName("Catalogs");
				catalogs.SelectedItem = catalogs.ItemsSource.Cast<Catalog>().First(n => n.Id == catalog.Id);
			});
			Input(view, "CatalogNames", Key.Enter);
			if (viewModel.Catalogs.Value.Count > 1)
				Input(view, "Catalogs", Key.Enter);
			AdvanceScheduler(3000);
			dispatcher.Invoke(() =>	{
				var element = (FrameworkElement)((Screen)shell.ActiveItem).GetView();
				var grid = (DataGrid)element.FindName("HistoryOrders");
				Assert.That(grid.Items.Count, Is.GreaterThan(0));
			});
		}

		[Test]
		public void Dynamic_recalculate_markup_validation()
		{
			StartWait();
			AsyncClick("ShowSettings");
			dispatcher.Invoke(() =>	{
				var content = (FrameworkElement)activeWindow.Content;
				var tab = (TabItem)content.FindName("VitallyImportantMarkupsTab");
				tab.IsSelected = true;
			});
			WaitIdle();
			dispatcher.Invoke(() =>	{
				var content = (FrameworkElement)activeWindow.Content;
				var grid = (DataGrid)content.FindName("VitallyImportantMarkups");
				EditCell(grid, 0, 1, "30");
				Assert.AreEqual(Color.FromRgb(0x80, 0x80, 0).ToString(), GetCell(grid, 0, 1).Background.ToString());
			});
		}

		[Test]
		public void Update_catalog_info()
		{
			StartWait();
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
			StartWait();
			AsyncClick("Update");

			WaitWindow("АналитФАРМАЦИЯ: Внимание");
			dispatcher.Invoke(() => {
				Assert.That(activeWindow.AsText(),
					Does.Contain("обнаружены препараты," +
						" предписанные к изъятию, в имеющихся у Вас электронных накладных"));
			});
			Click("ShowRejects");

			var model = (WaybillsViewModel)shell.ActiveItem;
			var view = (FrameworkElement)model.GetView();
			AdvanceScheduler(100);
			dispatcher.Invoke(() => {
				var waybills = (DataGrid)view.FindName("Waybills");
				Assert.AreEqual(1, waybills.Items.Count);
				Input(waybills, Key.Enter);
			});
			var details = (WaybillDetails)shell.ActiveItem;
			view = (FrameworkElement)details.GetView();
			dispatcher.Invoke(() => {
				var lines = view.Descendants().OfType<DataGrid>().First(x => x.Name == "Lines");
				lines.SelectedItem = lines.ItemsSource.OfType<WaybillLine>().First(x => x.IsRejectNew);
				scheduler.AdvanceByMs(500);
			});
			details.WaitQueryDrain().Wait();
			WaitIdle();
			dispatcher.Invoke(() => {
				var panel = (FrameworkElement)view.FindName("RejectPanel");
				Assert.IsTrue(details.IsRejectVisible.Value);
				AssertEndUserVisible(view, panel);
			});
		}

		private static void AssertEndUserVisible(FrameworkElement parent, FrameworkElement el)
		{
			Assert.IsTrue(el.IsVisible);
			var hitTestResult = VisualTreeHelper.HitTest(parent, el.TransformToAncestor(parent).Transform(new Point(0, 0)));
			Assert.AreEqual(hitTestResult.VisualHit, el);
		}

		[Test]
		public void Delay_of_payment()
		{
			//нужно что бы отработала логика в StartCheck
			settings.LastLeaderCalculation = DateTime.MinValue;
			Fixture<LocalDelayOfPayment>();
			var waitWindowAsync = manager.WindowOpened.Where(w => w.AsText().Contains("Пересчет отсрочки платежа")).Replay();
			disposable.Add(waitWindowAsync.Connect());
			StartWait();

			var waitWindow = waitWindowAsync.Timeout(10.Second()).First();
			Assert.That(waitWindow.AsText(), Does.Contain("Пересчет отсрочки платежа"));
			//ждем пока закроется
			WaitHelper.WaitOrFail(10.Second(), () => activeWindow != waitWindow);

			Click("ShowCatalog");
			OpenOffers();

			dispatcher.Invoke(() =>	{
				var offers = activeWindow.Descendants<AnalitFContlos.WinFormDataGrid>().First(g => g.WinFormDataGridName == "Offers");
				var supplierCost = GetCell(offers, "Цена поставщика");
				var cost = GetCell(offers, "Цена");
				Assert.AreNotEqual(supplierCost.Value.ToString(), cost.Value.ToString());
			});
		}

		[Test Ignore("тест конфликтует с WinForm.DataGridView")]
		public void ProducerPromotion()
		{
			session.DeleteEach<ProducerPromotion>();
			var fixture = new LocalProducerPromotion("assets/Валемидин.JPG");

			Fixture(fixture);

			StartWait();

			Click("ShowCatalog");

			OpenOffers(fixture.ProducerPromotion.Catalogs.First());

			AdvanceScheduler(500);

			dispatcher.Invoke(() =>
			{
				var producerPromotions = activeWindow.Descendants<ProducerPromotionPopup>().First();
				Assert.IsTrue(producerPromotions.IsVisible);
				Assert.That(producerPromotions.AsText(), Does.Contain(fixture.ProducerPromotion.Name));

				var presenter = producerPromotions.Descendants<ContentPresenter>()
					.First(x => x.DataContext is ProducerPromotion && ((ProducerPromotion)x.DataContext).Id == fixture.ProducerPromotion.Id);

				var link = presenter.Descendants<TextBlock>().SelectMany(x => x.Inlines).OfType<Hyperlink>().First();
				dispatcher.BeginInvoke(new Action(() => InternalClick(link)));
			});

			WaitWindow(fixture.ProducerPromotion.DisplayName);

			Thread.Sleep(50000);
			dispatcher.Invoke(() =>	{

				var viewer = activeWindow.Descendants<FlowDocumentScrollViewer>().First();

				var image = viewer.Document.Descendants<Image>().First();

				Assert.IsNotNull(image);

				Assert.That(image.Source.Height, Is.GreaterThan(0));

			});
		}

		[Test Ignore("тест конфликтует с WinForm.DataGridView")]
		public void Promotion()
		{
			session.DeleteEach<Promotion>();
			var fixture = new LocalPromotion("assets/Валемидин.JPG");
			Fixture(fixture);

			StartWait();
			Click("ShowCatalog");
			OpenOffers(fixture.Promotion.Catalogs[0]);
			AdvanceScheduler(500);
			dispatcher.Invoke(() =>
			{
				var promotions = activeWindow.Descendants<PromotionPopup>().First();
				Assert.IsTrue(promotions.IsVisible);
				Assert.That(promotions.AsText(), Does.Contain(fixture.Promotion.Name));
				var presenter = promotions.Descendants<ContentPresenter>()
					.First(c => c.DataContext is Promotion && ((Promotion)c.DataContext).Id == fixture.Promotion.Id);
				var link = presenter.Descendants<TextBlock>().SelectMany(b => b.Inlines).OfType<Hyperlink>().First();
				dispatcher.BeginInvoke(new Action(() => InternalClick(link)));
			});

			WaitWindow(fixture.Promotion.DisplayName);
			Thread.Sleep(50000);
			dispatcher.Invoke(() =>	{
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
				ProductIds = new[] {
					session.Query<Offer>().First().ProductId
				}
			};
			Fixture(fixture);
			var filename = TempFile("batch.txt", "1|10");
			session.DeleteEach<Order>();
			//что бы избежать сообщения о ожидаемых позициях
			session.DeleteEach<AwaitedItem>();

			StartWait();
			Click("ShowBatch");

			manager.OsDialog.OfType<OpenFileDialog>().Take(1)
				.Subscribe(d => d.FileName = Path.GetFullPath(filename));
			AsyncClickNoWait("Upload");

			WaitWindow("Обмен данными");
			WaitMessageBox("Автоматическая обработка дефектуры завершена.");
			WaitIdle();

			dispatcher.Invoke(() => {
				var items = activeWindow.Descendants<DataGrid>().First(g => g.Name == "ReportLines");
				Assert.That(items.Items.Count, Is.GreaterThan(0));
			});
		}

		[Test]
		public void Open_sent_orders()
		{
			var order = MakeSentOrder();
			StartWait();
			Click("ShowOrders");

			WaitIdle();
			dispatcher.Invoke(() => {
				var orders = (OrdersViewModel)shell.ActiveItem;
				orders.IsCurrentSelected.Value = false;
				orders.IsSentSelected.Value = true;
			});
			WaitIdle();
			dispatcher.Invoke(() => {
				var sentOrder = activeWindow.Descendants<DataGrid>().First(g => g.Name == "SentOrders");
				sentOrder.SelectedItem = sentOrder.Items.OfType<SentOrder>().First(o => o.Id == order.Id);
			});
			Input("SentOrders", Key.Enter);
			WaitIdle();
			dispatcher.Invoke(() => {
				var lines = activeWindow.Descendants<DataGrid>().First(g => g.Name == "Lines");
				var cell = GetCell(lines, "Цена");
				Assert.AreEqual(order.Lines[0].Cost.ToString("0.00", CultureInfo.InvariantCulture), cell.AsText());
			});
		}

		[Test]
		public void Schedule()
		{
			session.DeleteEach<Order>();
			restore = true;
			var fixture = new CreateSchedule {
				Schedules = new[] {
					DateTime.Now.AddMinutes(30).TimeOfDay
				}
			};
			Fixture(fixture);

			StartWait();

			dispatcher.Invoke(() => {
				SystemTime.Now = () => DateTime.Now.AddMinutes(20);
				scheduler.AdvanceByMs(30000);
			});

			AsyncClick("Update");
			WaitMessageBox("Обновление завершено успешно.");
			WaitIdle();

			dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				SystemTime.Now = () => DateTime.Now.AddMinutes(40);
				scheduler.AdvanceByMs(30000);
			}));

			WaitWindow("Обновление");
			dispatcher.Invoke(() => {
				Assert.That(activeWindow.AsText(),
					Does.Contain("Сейчас будет произведено обновление данных"));
			});
			AsyncClick("TryClose");
			WaitMessageBox("Обновление завершено успешно.");
		}

		[Test]
		public void Send_feedback()
		{
			StartWait();
			OpenMenu("Сервис");
			WaitIdle();
			AsyncClickMenu("Отправить письмо в АналитФармация");
			WaitWindow("Письмо в АналитФармация");
			InputActiveWindow("Subject", "test");
			Click("Send");
			WaitMessageBox("Письмо отправлено.");
		}

		[Test]
		public void Edit_style()
		{
			session.DeleteEach<Order>();
			MakeOrder();
			var junkOrder = MakeOrder(session.Query<Offer>().First(o => o.Junk));

			StartWait();
			dispatcher.Invoke(() => {
				session.DeleteEach<CustomStyle>();
				var styles = StyleHelper.GetDefaultStyles();
				session.SaveEach(styles);
				StyleHelper.BuildStyles(App.Current.Resources, styles);
			});

			Click("ShowOrderLines");
			dispatcher.BeginInvoke(new Action(() => {
				var el = activeWindow.Descendants<Panel>().Where(p => p.Name == "Legend")
					.SelectMany(p => p.Descendants<Label>())
					.First(i => i.Name == "JunkLegendItem");
				DoubleClick(el);
			}));
			var dialog = manager.OsDialog.OfType<ColorDialog>().Timeout(2.Second())
				.Take(1)
				.Do(d => {
					d.Color = System.Drawing.Color.MistyRose;
				})
				.First();
			Assert.IsNotNull(dialog);
			WaitIdle();
			dispatcher.Invoke(() => scheduler.Start());
			WaitIdle();

			dispatcher.Invoke(() => {
				var el = activeWindow.Descendants<Panel>().Where(p => p.Name == "Legend")
					.SelectMany(p => p.Descendants<Label>())
					.First(i => i.Name == "JunkLegendItem");
				Assert.AreEqual(System.Drawing.Color.MistyRose.ToHexString(), el.Background.ToString());

				var grid = activeWindow.Descendants<DataGrid>().First(g => g.Name == "Lines");
				//нужно убедиться что строку которую проверяем не выделена иначе цвета не совпадут из-за смешения
				grid.SelectedItem = grid.ItemsSource.OfType<OrderLine>().First(l => l.Id != junkOrder.Lines[0].Id);
				var cells = grid.Descendants<DataGridCell>()
					.Where(c => ((OrderLine)c.DataContext).Id == junkOrder.Lines[0].Id)
					.Where(c => ((TextBlock)c.Column.Header).Text == "Срок годн.")
					.ToArray();
				Assert.AreEqual(1, cells.Length);
				Assert.AreEqual(System.Drawing.Color.MistyRose.ToHexString(), cells[0].Background.ToString());
			});
		}

		[Test]
		public void Show_order_restore_result_after_update_notification()
		{
			session.DeleteEach<Order>();
			SimpleFixture.CreateCleanAwaited(session);
			MakeOrder();
			Fixture<RandCost>();

			StartWait();
			AsyncClick("Update");

			WaitWindow("АналитФАРМАЦИЯ: Внимание", "появились препараты, которые включены Вами в список ожидаемых позиций");
			AsyncClickNoWait("TryClose");
			WaitWindow("Корректировка восстановленных заказов");

			WaitIdle();
			ShallowBindingErrors();
		}

		[Test]
		public void Print_waybill()
		{
			Fixture<LocalWaybill>();
			StartWait();
			Click("ShowWaybills");
			AdvanceScheduler(100);

			Input("Waybills", Key.Return);
			WaitIdle();
			AsyncClickNoWait("PrintWaybill");
			WaitWindow("Настройка печати накладной");
			AsyncClickNoWait("OK");
			WaitWindow("Предварительный просмотр");
		}

		private void AdvanceScheduler(int milliseconds)
		{
			dispatcher.Invoke(() => { scheduler.AdvanceByMs(milliseconds); });
			WaitIdle();
		}

		[Test]
		public async Task Create_waybill()
		{
			await Start();
			Click("ShowWaybills");
			AsyncClick("Create");
			WaitWindow("Создание накладной");
			var id = Guid.NewGuid().ToString();
			dispatcher.Invoke(() => {
				Input(activeWindow.Descendants<FrameworkElement>().First(x => x.Name == "Waybill_ProviderDocumentId"), id);
				activeWindow.Descendants<System.Windows.Controls.ComboBox>().First(x => x.Name == "SupplierName").Text = "Test Supplier";
			});
			Click("OK");
			dispatcher.Invoke(() => {
				var model = (WaybillsViewModel)shell.ActiveItem;
				var waybill = model.Waybills.Value.First(x => x.ProviderDocumentId == id);
				model.CurrentWaybill.Value = waybill;
				model.EnterWaybill();
			});

			await ViewLoaded((Screen)shell.ActiveItem);
			WaitIdle();

			dispatcher.Invoke(() => {
				var grid = activeWindow.Descendants().OfType<DataGrid>().First(x => x.Name == "Lines");
				Assert.IsTrue(grid.CanUserAddRows);
				Assert.IsTrue(grid.CanUserDeleteRows);
				EditCell(grid, "Цена поставщика без НДС", 0, "500");
				EditCell(grid, "Цена поставщика с НДС", 0, "510");
				EditCell(grid, "НДС", 0, "10");
				Assert.AreEqual("620.00", GetCell(grid, "Розничная цена").AsText());
			});
		}

		private static void ShallowBindingErrors()
		{
			//на форме корректировки могут возникнуть ошибки биндинга
			//судя по обсуждению это ошибки wpf и они безобидны
			//http://wpf.codeplex.com/discussions/47047
			//игнорирую их
			var ignored = new[] {
				"System.Windows.Data Error: 4",
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=AreRowDetailsFrozen; DataItem=null; target element is 'DataGridDetailsPresenter' (Name=''); target property is 'SelectiveScrollingOrientation' (type 'SelectiveScrollingOrientation')",
				"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.DataGrid', AncestorLevel='1''. BindingExpression:Path=HeadersVisibility; DataItem=null; target element is 'DataGridRowHeader' (Name=''); target property is 'Visibility' (type 'Visibility')",
			};
			ViewSetup.BindingErrors.RemoveAll(s => ignored.Any(m => s.Contains(m)));
		}

		private void OpenMenu(string header)
		{
			dispatcher.Invoke(() => {
				var el = activeWindow.Descendants<Menu>().SelectMany(m => m.Items.OfType<MenuItem>())
					.Flat(i => i.Items.OfType<MenuItem>())
					.FirstOrDefault(i => header.Equals(i.Header));
				if (el == null)
					throw new Exception($"Не могу найти пункт меню с заголовком '{header}' в окне {activeWindow}");
				AssertInputable(el);

				//черная магия - когда IsSubmenuOpen = true
				//пункт меню пытается захватить ввод с мыши и если у него это не получится закрывает меню
				//на самом деле ввод захватить получится но не получится получить позицию курсора
				//из-за ошибки Win32Exception (0x80004005): Эта операция требует интерактивного оконного терминала
				//WinApi.SendMessage - нужен для того что бы обмануть HwndMouseInputProvider
				//обработав это сообщение он посчитает что активация произошла и не будет получать позицию курсора
				//WinApi.SetCapture - нужен для того что бы обмануть код активации, там происходит аналогичное получение позиции курсора
				//но если захватить ввод мыши получение координат курсора производиться не будет
				var windowHandle = new WindowInteropHelper(activeWindow).Handle;
				WinApi.SetCapture(windowHandle);
				WinApi.SendMessage(windowHandle, /*WM_MOUSEMOVE = */512u, 0, (IntPtr)0);
				WinApi.ReleaseCapture(windowHandle);

				el.IsSubmenuOpen = true;
			});
		}

		private void AsyncClickMenu(string header)
		{
			dispatcher.BeginInvoke(new Action(() => {
				var el = activeWindow.Descendants<Menu>().SelectMany(m => m.Items.OfType<MenuItem>())
					.Flat(i => i.Items.OfType<MenuItem>())
					.FirstOrDefault(i => header.Equals(i.Header));
				if (el == null)
					throw new Exception($"Не могу найти пункт меню с заголовком '{header}' в окне {activeWindow}");
				AssertInputable(el);
				el.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, el));
			}));
		}

		private void WaitWindow(string title, string body = null)
		{
			var found = false;
			dispatcher.Invoke(() => {
				found = activeWindow.Title == title;
			});
			if (found)
				return;
			var opened = manager.WindowOpened.Timeout(30.Second()).First();
			opened.Dispatcher.Invoke(() => {
				var text = opened.AsText();
				Assert.AreEqual(title, opened.Title, text);
				if (!String.IsNullOrEmpty(body))
					Assert.That(text, Does.Contain(body), text);
			});
		}

		private void WaitMessageBox(string message)
		{
			var timeout = 30.Second();
			if (IsCI())
				timeout = 60.Second();

			try {
				var opened = manager.MessageOpened.Timeout(timeout).First();
				Assert.AreEqual(opened, message);
				var window = WinApi.FindWindow(IntPtr.Zero, "АналитФАРМАЦИЯ: Информация");
				for(var i = 0; window == IntPtr.Zero && i < 100; i++) {
					Thread.Sleep(20);
					window = WinApi.FindWindow(IntPtr.Zero, "АналитФАРМАЦИЯ: Информация");
				}
				if (window == IntPtr.Zero)
					throw new Exception(string.Format("Не удалось найти окно '{0}'", "АналитФАРМАЦИЯ: Информация"));
				WinApi.SendMessage(window, WinApi.WM_CLOSE, 0, IntPtr.Zero);
			}
			catch(TimeoutException e) {
				throw new Exception($"Не удалось дождаться {message}, окно {activeWindow.AsText()}", e);
			}
		}

		private void EditCell(DataGrid grid, string column, int row, string text)
		{
			EditCell(grid, DataGridHelper.FindColumn(grid, column).DisplayIndex, row, text);
		}

		private void EditCell(DataGrid grid, int column, int row, string text)
		{
			var cell = GetCell(grid, column, row);
			cell.Focus();
			Input(cell, Key.F2);
			if (cell.DataContext.ToString() == "{DataGrid.NewItemPlaceholder}") {
				cell = GetCell(grid, column, row);
				Input(cell, Key.F2);
			}
			var edit = cell.Descendants<TextBox>().First();
			Input(edit, text);
			Input(cell, Key.Enter);
		}

		private DataGridCell GetCell(DataGrid grid, string name, int row = 0)
		{
			var column = DataGridHelper.FindColumn(grid, name);
			return GetCell(grid, column.DisplayIndex, row);
		}

		private DataGridCell GetCell(DataGrid grid, int column, int row)
		{
			var gridRow = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(row);
			var presenter = gridRow.VisualChild<DataGridCellsPresenter>();
			return (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
		}

		private DataGridViewCell GetCell(AnalitFContlos.WinFormDataGrid grid, int column, int row)
		{
			return (DataGridViewCell)grid.DataGrid.Grid.Rows[row].Cells[column];
		}

		private DataGridViewCell GetCell(AnalitFContlos.WinFormDataGrid grid, string name, int row = 0)
		{
			int columnIndex = -1;
			foreach (DataGridViewColumn c in grid.DataGrid.Grid.Columns)

			{
				if (c.HeaderText == name)
				{
					columnIndex = c.DisplayIndex;
					break;
				}
			}
			return GetCell(grid, columnIndex, row);
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
				throw new Exception($"Не удалось получить view из {viewModel.GetType()}");
			if (catalog != null) {
				dispatcher.Invoke(() => {
					var names = activeWindow.Descendants<DataGrid>().First(g => g.Name == "CatalogNames");
					names.SelectedItem = names.ItemsSource.Cast<CatalogName>().First(c => c.Id == catalog.Name.Id);
				});
			}
			WaitIdle();
			Input(view, "CatalogNames", Key.Enter);
			Assert.That(viewModel.Catalogs.Value.Count, Is.GreaterThan(0),
				"нет ни одной формы выпуска, для {0}", viewModel.CurrentCatalogName.Value);
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
				throw new Exception(
					$"Не удалось дождаться модели {typeof (T)} текущая модель {shell.ActiveItem?.GetType().ToString() ?? "null"}");

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
