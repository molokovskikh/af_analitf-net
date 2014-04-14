using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq.Observαble;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using Caliburn.Micro;
using Common.Tools.Calendar;
using Microsoft.Test.Input;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using Remotion.Linq.Parsing;
using Action = System.Action;
using Keyboard = System.Windows.Input.Keyboard;
using Mouse = Microsoft.Test.Input.Mouse;
using MouseButton = Microsoft.Test.Input.MouseButton;
using WpfHelper = AnalitF.Net.Client.Helpers.WpfHelper;

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
			Client.Test.TestHelpers.WpfHelper.WithWindow(w => {
				var selector = new PopupSelector();
				selector.Name = "Items";
				selector.Member = "Item.Item2";
				w.Content = selector;
				selector.Loaded += (sender, args) => {
					var text = WpfHelper.AsText(selector);
					Assert.That(text, Is.StringContaining("test2"));

					Client.Test.TestHelpers.WpfHelper.Shutdown(w);
				};
				w.DataContext = new Model();
				ViewModelBinder.Bind(w.DataContext, w, null);
			});
		}

		[Test]
		public void Popup_scroll()
		{
			Client.Test.TestHelpers.WpfHelper.WithWindow(w => {
				var selector = InitSelector(w);

				selector.Loaded += (sender, args) => {
					selector.IsOpened = true;
					w.Dispatcher.InvokeAsync(() => {
						var scrollViewer = selector.Descendants<ScrollViewer>().First();
						Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedVerticalScrollBarVisibility);
						Client.Test.TestHelpers.WpfHelper.Shutdown(w);
					}, DispatcherPriority.ContextIdle);
				};
			});
		}

		[Test]
		public void Filter_label()
		{
			Client.Test.TestHelpers.WpfHelper.WithWindow(w => {
				var selector = InitSelector(w);

				selector.Loaded += (sender, args) => {
					var stackPanel = selector.Descendants<Grid>().FirstOrDefault(g => g.Name == "MainGrid").Descendants<StackPanel>().First();
					Assert.That(stackPanel.AsText(), Is.Not.StringContaining(", фильтр применен"));
					selector.IsOpened = true;
					((ISelectable)selector.Items[0]).IsSelected = false;
					Assert.That(stackPanel.AsText(), Is.StringContaining(", фильтр применен"));
					Client.Test.TestHelpers.WpfHelper.Shutdown(w);
				};
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
			Client.Test.TestHelpers.WpfHelper.WithWindow(w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
				grid.Loaded += (sender, args) => {
					DataGridHelper.Focus(grid);
					grid.ItemsSource = Enumerable.Range(500, 100).Select(i => Tuple.Create(i.ToString())).ToList();
					w.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
						Assert.IsTrue(grid.IsKeyboardFocusWithin);
						Assert.IsInstanceOf<DataGridCell>(Keyboard.FocusedElement);

						Client.Test.TestHelpers.WpfHelper.Shutdown(w);
					}));
				};
			});
		}

		[Test, Explicit("тест управляет мышкой и движения пользователя могут его сломать")]
		public void Set_focus_on_empty_grid()
		{
			Client.Test.TestHelpers.WpfHelper.WithWindow(w => {
				var grid = new DataGrid2();
				w.Content = grid;
				grid.Loaded += (sender, args) => {
					Assert.IsFalse(grid.IsKeyboardFocusWithin);
					var point = grid.PointToScreen(new Point(3, 3));
					Mouse.MoveTo(new System.Drawing.Point((int)point.X, (int)point.Y));
					Mouse.Click(MouseButton.Left);
					Idle(w, () => {
						Assert.IsTrue(grid.IsKeyboardFocusWithin);
						Client.Test.TestHelpers.WpfHelper.Shutdown(w);
					});
				};
			});
		}

		[Test]
		public void Focus_on_empty_data_grid()
		{
			Client.Test.TestHelpers.WpfHelper.WithWindow(w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = Enumerable.Empty<Tuple<string>>().ToList();
				Idle(w, () => {
					DataGridHelper.Focus(grid);
					Assert.IsTrue(grid.IsKeyboardFocusWithin);
					Client.Test.TestHelpers.WpfHelper.Shutdown(w);
				});
			});
		}

		[Test]
		public void Do_not_lose_focus_on_delete()
		{
			var items = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
			var data = new ObservableCollection<Tuple<String>>(items);
			Client.Test.TestHelpers.WpfHelper.WithWindow(w => {
				var grid = new DataGrid2();
				grid.AutoGenerateColumns = false;
				grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("Items") });
				w.Content = grid;
				grid.ItemsSource = data;
				Idle(w, () => {
					DataGridHelper.Focus(grid);
					Assert.IsNotNull(grid.CurrentItem);
					data.Remove((Tuple<string>)grid.SelectedItem);

					Idle(w, () => {
						Assert.IsTrue(grid.IsKeyboardFocusWithin);
						Assert.IsNotNull(grid.CurrentItem);
						Client.Test.TestHelpers.WpfHelper.Shutdown(w);
					});
				});
			});
		}

		private static void Idle(Window w, Action target)
		{
			w.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(target));
		}
	}
}