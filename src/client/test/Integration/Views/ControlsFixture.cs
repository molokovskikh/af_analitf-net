using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using Caliburn.Micro;
using Common.Tools.Calendar;
using NUnit.Framework;
using Keyboard = System.Windows.Input.Keyboard;
using Mouse = Microsoft.Test.Input.Mouse;
using MouseButton = Microsoft.Test.Input.MouseButton;
using Point = System.Windows.Point;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class ControlsFixture
	{
		public class Model
		{
			public Model()
			{
				Items = new List<Selectable<Tuple<string, string>>> {
					new Selectable<Tuple<string, string>>(Tuple.Create("test1", "test2"))
				};
			}

			public List<Selectable<Tuple<string, string>>> Items { get; set; }
		}

		[SetUp]
		public void Setup()
		{
			//нужны стили
			if (Application.Current == null)
				Application.LoadComponent(new Uri("/AnalitF.Net.Client;component/app.xaml", UriKind.Relative));
		}

		[Test, Explicit("тест конфликтует с пользовательским вводом")]
		public void Popup_selector()
		{
			WpfTestHelper.WithWindow(w => {
				var selector = new PopupSelector();
				selector.Name = "Items";
				selector.Member = "Item.Item2";
				w.Content = selector;
				selector.Loaded += (sender, args) => {
					var text = selector.AsText();
					Assert.That(text, Is.StringContaining("test2"));

					WpfTestHelper.Shutdown(w);
				};
				w.DataContext = new Model();
				ViewModelBinder.Bind(w.DataContext, w, null);
			});
		}

		[Test]
		public void Popup_scroll()
		{
			WpfTestHelper.WithWindow2(async w => {
				var selector = InitSelector(w);
				await selector.WaitLoaded();
				selector.IsOpened = true;
				await w.WaitIdle();
				var scrollViewer = selector.Descendants<ScrollViewer>().First();
				Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedVerticalScrollBarVisibility);
			});
		}

		[Test]
		public void Filter_label()
		{
			WpfTestHelper.WithWindow2(async w => {
				var selector = InitSelector(w);
				await selector.WaitLoaded();

				var stackPanel = selector.Descendants<Grid>().FirstOrDefault(g => g.Name == "MainGrid").Descendants<StackPanel>().First();
				Assert.That(stackPanel.AsText(), Is.Not.StringContaining(", фильтр применен"));
				selector.IsOpened = true;
				((ISelectable)selector.Items[0]).IsSelected = false;
				Assert.That(stackPanel.AsText(), Is.StringContaining(", фильтр применен"));
			});
		}

		private static PopupSelector InitSelector(Window w)
		{
			var selector = new PopupSelector();
			selector.Name = "Items";
			selector.Member = "Item.Item2";
			w.Content = new StackPanel {
				Children = { selector }
			};
			var model = new Model();
			model.Items = Enumerable.Range(1, 100)
				.Select(i => new Selectable<Tuple<string, string>>(Tuple.Create(i.ToString(), i.ToString())))
				.ToList();
			w.DataContext = model;
			ViewModelBinder.Bind(w.DataContext, w, null);
			return selector;
		}

		[Test]
		public void Do_not_reset_data_grid_focus()
		{
			WpfTestHelper.WithWindow2(async w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
				await grid.WaitLoaded();
				DataGridHelper.Focus(grid);
				grid.ItemsSource = Enumerable.Range(500, 100).Select(i => Tuple.Create(i.ToString())).ToList();
				await w.WaitIdle();
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				Assert.IsInstanceOf<DataGridCell>(Keyboard.FocusedElement);
			});
		}

		[Test, Explicit("тест управляет мышкой и движения пользователя могут его сломать")]
		public void Set_focus_on_empty_grid()
		{
			WpfTestHelper.WithWindow2(async w => {
				var grid = new DataGrid2();
				w.Content = grid;
				await grid.WaitLoaded();

				Assert.IsFalse(grid.IsKeyboardFocusWithin);
				var point = grid.PointToScreen(new Point(3, 3));
				Mouse.MoveTo(new System.Drawing.Point((int)point.X, (int)point.Y));
				Mouse.Click(MouseButton.Left);
				await w.WaitIdle();
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
			});
		}

		[Test]
		public void Focus_on_empty_data_grid()
		{
			WpfTestHelper.WithWindow2(async w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = Enumerable.Empty<Tuple<string>>().ToList();

				await w.WaitIdle();
				DataGridHelper.Focus(grid);
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
			});
		}

		[Test]
		public void Do_not_lose_focus_on_delete()
		{
			var items = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
			var data = new ObservableCollection<Tuple<String>>(items);
			WpfTestHelper.WithWindow2(async w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = data;

				await w.WaitIdle();
				DataGridHelper.Focus(grid);
				Assert.IsNotNull(grid.CurrentItem);
				data.Remove((Tuple<string>)grid.SelectedItem);

				await w.WaitIdle();
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				Assert.IsNotNull(grid.CurrentItem);
			});
		}

		[Test]
		public void Style_fixture()
		{
			var offers = new List<Offer>();
			offers.Add(new Offer(new Price("тест"), 100) {
				IsGrouped = true,
				BuyingMatrixType = BuyingMatrixStatus.Denied,
			});

			WpfTestHelper.WithWindow2(async w => {
				var resources = new ResourceDictionary();
				StyleHelper.Reset();
				StyleHelper.BuildStyles(resources);
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("ProductSynonym") });
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("OrderCount") });
				w.Content = grid;
				StyleHelper.ApplyStyles(typeof(Offer), grid, resources);
				grid.ItemsSource = offers;
				await w.WaitIdle();
				var cells = grid.Descendants<DataGridCell>().ToArray();
				Assert.That(cells.Length, Is.GreaterThan(0));
				foreach (var cell in cells)
					Assert.AreEqual("#FFFF0000", cell.Background.ToString(), ((TextBlock)cell.Content).Text);
			});
		}

		[Test]
		public void Scroll_on_wheel()
		{
			var items = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
			WpfTestHelper.WithWindow2(async w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Item1") });
				w.Content = grid;

				grid.RaiseEvent(new MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice,
					0,
					-System.Windows.Input.Mouse.MouseWheelDeltaForOneLine) {
						RoutedEvent = UIElement.PreviewMouseWheelEvent,
						Source = grid,
					});
				grid.ItemsSource = items;
				await grid.WaitIdle();
				grid.CurrentItem = items[0];
				grid.CurrentColumn = grid.Columns[0];
				grid.RaiseEvent(new MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice,
					0,
					-System.Windows.Input.Mouse.MouseWheelDeltaForOneLine) {
						RoutedEvent = UIElement.PreviewMouseWheelEvent,
						Source = grid,
					});
				Assert.AreEqual("2", ((Tuple<string>)grid.CurrentItem).Item1);

				await w.WaitIdle();
			});
		}

		public class SearchableModel
		{
			public SearchableModel()
			{
				Term = new NotifyValue<string>();
				Items = new NotifyValue<List<Tuple<string>>>();
			}

			public NotifyValue<List<Tuple<string>>> Items { get; set; }

			public NotifyValue<string> Term { get; set; }
		}

		[Test]
		public void Searchable_column()
		{
			WpfTestHelper.WithWindow2(async w => {
				var model = new SearchableModel();
				var grid = new DataGrid2();
				grid.DataContext = model;
				BindingOperations.SetBinding(grid, DataGrid.ItemsSourceProperty, new Binding("Items.Value"));
				BindingOperations.SetBinding(grid, SearchableDataGridColumn.SearchTermProperty, new Binding("Term.Value"));
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new SearchableDataGridColumn {
					Binding = new Binding("Item1"),
					HighlightStyle = new Style {
						Setters = {
							new Setter(TextElement.BackgroundProperty, Brushes.Red)
						}
					}
				});
				w.Content = grid;
				await grid.WaitLoaded();
				await w.WaitIdle();

				model.Items.Value = Enumerable.Range(0, 49).Select(i => Tuple.Create(i.ToString())).ToList();
				await w.WaitIdle();

				model.Term.Value = "5";
				model.Items.Value = Enumerable.Range(50, 100).Select(i => Tuple.Create(i.ToString())).ToList();
				await w.WaitIdle();

				var row = grid.Descendants<DataGridRow>().First(r => ((Tuple<String>)r.DataContext).Item1 == "50");
				var text = row.Descendants<TextBlock>().First();
				Assert.AreEqual("50", text.Text);
				var inlines = text.Inlines.OfType<Run>().ToArray();
				Assert.AreEqual("5", inlines[0].Text);
				Assert.AreEqual("0", inlines[1].Text);
			});
		}

		[Test]
		public void Revert_value_on_incorrect_input()
		{
			var waybill = new Waybill(new Address("тест"), new Supplier());
			var line = new WaybillLine(waybill);
			line.RetailCost = 150;
			waybill.AddLine(line);
			WpfTestHelper.WithWindow2(async w => {
				var grid = new DataGrid2();
				grid.IsReadOnly = false;
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumnEx { Binding = new Binding("Product") });
				grid.Columns.Add(new DataGridTextColumnEx { Binding = new Binding("RetailCost") });
				w.Content = grid;
				grid.ItemsSource = waybill.Lines;
				await w.WaitIdle();
				var cell = grid.Descendants<DataGridCell>()
					.First(x => x.Column == grid.Columns[1] && x.DataContext == line);
				Assert.IsTrue(cell.Focus());
				cell.SendKey(Key.F2);
				Assert.IsTrue(cell.IsEditing);
				var input = cell.Descendants<TextBox>().First();
				input.SendText("asd");
				grid.SendKey(Key.Down);
				Assert.IsFalse(cell.IsEditing);
				Assert.AreEqual(150, line.RetailCost);
			});
		}
	}
}