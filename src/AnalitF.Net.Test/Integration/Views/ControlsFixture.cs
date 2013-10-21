using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using NUnit.Framework;
using Action = System.Action;
using DataGrid = AnalitF.Net.Client.Controls.DataGrid;
using Keyboard = System.Windows.Input.Keyboard;
using Mouse = Microsoft.Test.Input.Mouse;
using MouseButton = Microsoft.Test.Input.MouseButton;

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

		[Test]
		public void Popup_selector()
		{
			WpfHelper.WithWindow(w => {
				var selector = new PopupSelector();
				selector.Name = "Items";
				selector.Member = "Item.Item2";
				w.Content = selector;
				selector.Loaded += (sender, args) => {
					var text = XamlExtentions.AsText(selector);
					Assert.That(text, Is.StringContaining("test2"));

					WpfHelper.Shutdown(w);
				};
				w.DataContext = new Model();
				ViewModelBinder.Bind(w.DataContext, w, null);
			});
		}

		[Test]
		public void Do_not_reset_data_grid_focus()
		{
			WpfHelper.WithWindow(w => {
				var grid = new DataGrid();
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

						WpfHelper.Shutdown(w);
					}));
				};
			});
		}

		[Test]
		public void Set_focus_on_empty_grid()
		{
			WpfHelper.WithWindow(w => {
				var grid = new DataGrid();
				w.Content = grid;
				grid.Loaded += (sender, args) => {
					Assert.IsFalse(grid.IsKeyboardFocusWithin);
					var point = grid.PointToScreen(new Point(3, 3));
					Mouse.MoveTo(new System.Drawing.Point((int)point.X, (int)point.Y));
					Mouse.Click(MouseButton.Left);
					Idle(w, () => {
						Assert.IsTrue(grid.IsKeyboardFocusWithin);
						WpfHelper.Shutdown(w);
					});
				};
			});
		}

		private static void Idle(Window w, Action target)
		{
			w.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(target));
		}

		[Test]
		public void Focus_on_empty_data_grid()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void Do_not_lose_focus_on_delete()
		{
			var items = Enumerable.Range(1, 100).Select(i => Tuple.Create(i.ToString())).ToList();
			var data = new ObservableCollection<Tuple<String>>(items);
			WpfHelper.WithWindow(w => {
				var grid = new DataGrid();
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
						WpfHelper.Shutdown(w);
					});
				});
			});
		}
	}
}