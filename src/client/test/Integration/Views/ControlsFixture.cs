using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using Caliburn.Micro;
using NUnit.Framework;
using Remotion.Linq.Parsing;
using Keyboard = System.Windows.Input.Keyboard;
using Mouse = Microsoft.Test.Input.Mouse;
using MouseButton = Microsoft.Test.Input.MouseButton;
using Point = System.Windows.Point;
using WpfHelper = AnalitF.Net.Client.Test.TestHelpers.WpfHelper;

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
			WpfHelper.WithWindow(w => {
				var selector = new PopupSelector();
				selector.Name = "Items";
				selector.Member = "Item.Item2";
				w.Content = selector;
				selector.Loaded += (sender, args) => {
					var text = selector.AsText();
					Assert.That(text, Is.StringContaining("test2"));

					WpfHelper.Shutdown(w);
				};
				w.DataContext = new Model();
				ViewModelBinder.Bind(w.DataContext, w, null);
			});
		}

		[Test]
		public void Popup_scroll()
		{
			WpfHelper.WithWindow(async w => {
				var selector = InitSelector(w);
				await selector.WaitLoaded();
				selector.IsOpened = true;
				await w.Dispatcher.WaitIdle();
				var scrollViewer = selector.Descendants<ScrollViewer>().First();
				Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedVerticalScrollBarVisibility);
				WpfHelper.Shutdown(w);
			});
		}

		[Test]
		public void Filter_label()
		{
			WpfHelper.WithWindow(async w => {
				var selector = InitSelector(w);
				await selector.WaitLoaded();

				var stackPanel = selector.Descendants<Grid>().FirstOrDefault(g => g.Name == "MainGrid").Descendants<StackPanel>().First();
				Assert.That(stackPanel.AsText(), Is.Not.StringContaining(", фильтр применен"));
				selector.IsOpened = true;
				((ISelectable)selector.Items[0]).IsSelected = false;
				Assert.That(stackPanel.AsText(), Is.StringContaining(", фильтр применен"));
				WpfHelper.Shutdown(w);
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
			WpfHelper.WithWindow(async w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
				await grid.WaitLoaded();
				DataGridHelper.Focus(grid);
				grid.ItemsSource = Enumerable.Range(500, 100).Select(i => Tuple.Create(i.ToString())).ToList();
				await w.Dispatcher.WaitIdle();
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				Assert.IsInstanceOf<DataGridCell>(Keyboard.FocusedElement);

				WpfHelper.Shutdown(w);
			});
		}

		[Test, Explicit("тест управляет мышкой и движения пользователя могут его сломать")]
		public void Set_focus_on_empty_grid()
		{
			WpfHelper.WithWindow(async w => {
				var grid = new DataGrid2();
				w.Content = grid;
				await grid.WaitLoaded();

				Assert.IsFalse(grid.IsKeyboardFocusWithin);
				var point = grid.PointToScreen(new Point(3, 3));
				Mouse.MoveTo(new System.Drawing.Point((int)point.X, (int)point.Y));
				Mouse.Click(MouseButton.Left);
				await w.Dispatcher.WaitIdle();
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				WpfHelper.Shutdown(w);
			});
		}

		[Test]
		public void Focus_on_empty_data_grid()
		{
			WpfHelper.WithWindow(async w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = Enumerable.Empty<Tuple<string>>().ToList();

				await w.Dispatcher.WaitIdle();
				DataGridHelper.Focus(grid);
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				WpfHelper.Shutdown(w);
			});
		}

		[Test]
		public void Do_not_lose_focus_on_delete()
		{
			var items = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
			var data = new ObservableCollection<Tuple<String>>(items);
			WpfHelper.WithWindow(async w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = data;

				await w.Dispatcher.WaitIdle();
				DataGridHelper.Focus(grid);
				Assert.IsNotNull(grid.CurrentItem);
				data.Remove((Tuple<string>)grid.SelectedItem);

				await w.Dispatcher.WaitIdle();
				Assert.IsTrue(grid.IsKeyboardFocusWithin);
				Assert.IsNotNull(grid.CurrentItem);
				WpfHelper.Shutdown(w);
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

			WpfHelper.WithWindow(async w => {
				var resources = new ResourceDictionary();
				StyleHelper.Reset();
				StyleHelper.BuildStyles(resources);
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("ProductSynonym") });
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("OrderCount") });
				w.Content = grid;
				grid.ItemsSource = offers;
				await w.Dispatcher.WaitIdle();
				var cells = grid.Children().OfType<DataGridCell>().ToArray();
				foreach (var cell in cells)
					Assert.AreEqual("Red", cell.Background.ToString(), cell.ToString());
				WpfHelper.Shutdown(w);
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
			WpfHelper.WithWindow(async w => {
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
				await w.Dispatcher.WaitIdle();

				model.Items.Value = Enumerable.Range(0, 49).Select(i => Tuple.Create(i.ToString())).ToList();
				await w.Dispatcher.WaitIdle();

				model.Term.Value = "5";
				model.Items.Value = Enumerable.Range(50, 100).Select(i => Tuple.Create(i.ToString())).ToList();
				await w.Dispatcher.WaitIdle();

				var row = grid.Descendants<DataGridRow>().First(r => ((Tuple<String>)r.DataContext).Item1 == "50");
				var text = row.Descendants<TextBlock>().First();
				Assert.AreEqual("50", text.Text);
				var inlines = text.Inlines.OfType<Run>().ToArray();
				Assert.AreEqual("5", inlines[0].Text);
				Assert.AreEqual("0", inlines[1].Text);
				WpfHelper.Shutdown(w);
			});
		}
	}
}