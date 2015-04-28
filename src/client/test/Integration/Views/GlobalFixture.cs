using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.Test.Unit;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Helpers;
using Microsoft.Test.CommandLineParsing;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI.Testing;
using Microsoft.Win32;
using TestStack.White.UIItems.TableItems;
using Screen = Caliburn.Micro.Screen;
using Action = System.Action;
using Address = AnalitF.Net.Client.Models.Address;
using Hyperlink = System.Windows.Documents.Hyperlink;

namespace AnalitF.Net.Test.Integration.Views
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
		public async void Open_catalog_on_quick_search()
		{
			Start();

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
		public async void Open_catalog_offers()
		{
			var term = session.Query<CatalogName>()
				.First(n => n.HaveOffers && n.Name.StartsWith("б"))
				.Name.Slice(3).ToLower();

			Start();
			Click("ShowCatalog");

			var catalog = await ViewLoaded<CatalogViewModel>();
			dispatcher.Invoke(() => {
				catalog.CatalogSearch.Value = true;
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
				DoubleClick(grid, selectMany.First());
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

			Start();
			Click("ShowWaybills");
			Input("Waybills", Key.Enter);
			WaitIdle();
			dispatcher.Invoke(() => {
				var view = (FrameworkElement)((WaybillDetails)shell.ActiveItem).GetView();
				var datagrid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
				var printColumn = datagrid.Columns.First(c => !(c.Header is TextBlock));
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
			var line = session.Query<OrderLine>().First(l => l.SendResult == LineResultStatus.CostChanged);
			dispatcher.Invoke(() => {
				var lines = activeWindow.Descendants<DataGrid>().First(x => x.Name == "Lines");
				var cells = lines.Descendants<DataGridCell>().Where(x => ((OrderLine)x.DataContext).Id == line.Id).ToArray();
				var oldcost = cells.First(x => ((Binding)((DataGridBoundColumn)x.Column).Binding).Path.Path == "MixedOldCost");
				var newcost = cells.First(x => ((Binding)((DataGridBoundColumn)x.Column).Binding).Path.Path == "MixedNewCost");
				if (line.IsCostDecreased) {
					Assert.AreEqual("#FFCDEAB9", oldcost.Background.ToString());
					Assert.AreEqual("#FFCDEAB9", newcost.Background.ToString());
				}
				else {
					//цвет может быть смешаный если строка выбрана или не смешаный если строка не выбрана
					Assert.That(oldcost.Background.ToString(), Is.EqualTo("#FFE3B4BA").Or.EqualTo("#FFEF5275"));
					Assert.That(newcost.Background.ToString(), Is.EqualTo("#FFE3B4BA").Or.EqualTo("#FFEF5275"));
				}
			});

			WaitIdle();
			ShallowBindingErrors();
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
		public void Show_price_offers()
		{
			Start();
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
			dispatcher.Invoke(() => testScheduler.Start());
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
			Start();
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
					Is.StringContaining("обнаружены препараты," +
						" предписанные к изъятию, в имеющихся у Вас электронных накладных"));
			});
			Click("ShowRejects");

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
			//нужно что бы отработала логика в StartCheck
			settings.LastLeaderCalculation = DateTime.MinValue;
			Fixture<LocalDelayOfPayment>();
			var waitWindowAsync = manager.WindowOpened.Where(w => w.AsText().Contains("Пересчет отсрочки платежа")).Replay();
			disposable.Add(waitWindowAsync.Connect());
			Start();

			var waitWindow = waitWindowAsync.Timeout(10.Second()).First();
			Assert.That(waitWindow.AsText(), Is.StringContaining("Пересчет отсрочки платежа"));
			//ждем пока закроется
			WaitHelper.WaitOrFail(10.Second(), () => activeWindow != waitWindow);

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
				ProductIds = new[] {
					session.Query<Offer>().First().ProductId
				}
			};
			Fixture(fixture);
			var filename = TempFile("batch.txt", "1|10");
			session.DeleteEach<Order>();
			//что бы избежать сообщения о ожидаемых позициях
			session.DeleteEach<AwaitedItem>();

			Start();
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
			Start();
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

			Start();

			SystemTime.Now = () => DateTime.Now.AddMinutes(20);
			testScheduler.AdvanceByMs(30000);

			AsyncClick("Update");
			WaitMessageBox("Обновление завершено успешно.");
			WaitIdle();

			dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				SystemTime.Now = () => DateTime.Now.AddMinutes(40);
				testScheduler.AdvanceByMs(30000);
			}));

			WaitWindow("Обновление");
			dispatcher.Invoke(() => {
				Assert.That(activeWindow.AsText(),
					Is.StringContaining("Сейчас будет произведено обновление данных"));
			});
			AsyncClick("TryClose");
			WaitMessageBox("Обновление завершено успешно.");
		}

		[Test]
		public void Send_feedback()
		{
			Start();
			OpenMenu("Сервис");
			WaitIdle();
			AsyncClickMenu("Отправить письмо в АК \"Инфорум\"");
			WaitWindow("Письмо в АК \"Инфорум\"");
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

			Start();
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
			var dialog = manager.OsDialog.OfType<System.Windows.Forms.ColorDialog>().Timeout(2.Second())
				.Take(1)
				.Do(d => {
					d.Color = System.Drawing.Color.MistyRose;
				})
				.First();
			Assert.IsNotNull(dialog);
			WaitIdle();
			dispatcher.Invoke(() => testScheduler.Start());
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
			SimpleFixture.CreateCleanAwaited(session);
			MakeOrder();
			Fixture<RandCost>();

			Start();
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
			Start();
			Click("ShowWaybills");

			Input("Waybills", Key.Return);
			WaitIdle();
			AsyncClickNoWait("PrintWaybill");
			WaitWindow("Настройка печати накладной");
			AsyncClickNoWait("OK");
			WaitWindow("Предварительный просмотр");
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
					throw new Exception(String.Format("Не могу найти пункт меню с заголовком '{0}' в окне {1}", header, activeWindow));
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
					throw new Exception(String.Format("Не могу найти пункт меню с заголовком '{0}' в окне {1}", header, activeWindow));
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
					Assert.That(text, Is.StringContaining(body), text);
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
					throw new Exception(String.Format("Не удалось найти окно '{0}'", "АналитФАРМАЦИЯ: Информация"));
				WinApi.SendMessage(window, WinApi.WM_CLOSE, 0, IntPtr.Zero);
			}
			catch(TimeoutException e) {
				throw new Exception(String.Format("Не удалось дождаться {0}, окно {1}", message, activeWindow.AsText()), e);
			}
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
			var column = DataGridHelper.FindColumn(grid, name);
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
