using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Loader.Entity;
using ReactiveUI.Xaml;

namespace AnalitF.Net.Client.Binders
{
	public class QuickSearchBehavior
	{
		public static DependencyProperty GridRef
			= DependencyProperty.RegisterAttached("GridRef",
				typeof(Control),
				typeof(QuickSearchBehavior));

		public static void SetGridRef(DependencyObject d, Control source)
		{
			d.SetValue(GridRef, source);
		}

		public static DataGrid GetGridRef(DependencyObject d)
		{
			return d.GetValue(GridRef) as DataGrid;
		}

		//основная идея:
		//видимость поля ввода привязана к поисковому запросу
		//если запрос есть то поле видимо
		//если поле видимо то мы подписываемся на события
		//изменения ячейки и - что бы знать когда нужно изменить
		//подписку на потерю фокуса
		//потери фокуса ячейки - что бы знать что человек ушел из таблицы
		//и поиск нужно сбросить
		//если поле стало скрытым все подписки надо освободить
		public static void AttachSearch(DataGrid grid, TextBox searchText)
		{
			grid.ObservableTextInput()
				.CatchSubscribe(e => RedirectInput(e, searchText));

			var binding = BindingOperations.GetBinding(grid, Selector.SelectedItemProperty);
			//если биндинга нет это странно
			//магия что бы обойти ошибку в .net 4.0 см комментарий к CurrentItemStubProperty
			if (binding != null) {
				BindingOperations.SetBinding(grid,
					Controls.DataGrid2.CurrentItemStubProperty,
					new Binding {
						Path = new PropertyPath(binding.Path.Path, binding.Path.PathParameters),
					});
			}

			grid.KeyDown += (sender, args) => {
				if (!searchText.IsEnabled)
					return;

				if (args.Key == Key.Escape) {
					if (!String.IsNullOrEmpty(searchText.Text)) {
						args.Handled = true;
						searchText.Text = null;
					}
				}
			};

			var disposable = new CompositeDisposable();

			searchText.IsVisibleChanged += (sender, args) => {
				if ((bool)args.NewValue) {
					var cellChangedSubscription = Observable
						.FromEventPattern<EventArgs>(grid, "CurrentCellChanged")
						.Subscribe(_ => AttachToCurrentCell(grid, disposable, searchText));

					disposable.Add(cellChangedSubscription);

					if (!((bool)grid.GetValue(Selector.IsSelectionActiveProperty)))
						DataGridHelper.Focus(grid);

					AttachToCurrentCell(grid, disposable, searchText);
				}
				else {
					disposable.Dispose();
					disposable = new CompositeDisposable();
				}
			};
		}

		public static void RedirectInput(EventPattern<TextCompositionEventArgs> message, TextBox searchText)
		{
			if (!searchText.IsEnabled)
				return;
			if (String.IsNullOrEmpty(message.EventArgs.Text))
				return;

			if (!Char.IsControl(message.EventArgs.Text[0])) {
				message.EventArgs.Handled = true;
				searchText.Text += message.EventArgs.Text;
				DataGridHelper.Centrify(message.Sender as DataGrid);
			}
			else if (message.EventArgs.Text[0] == '\b') {
				message.EventArgs.Handled = true;
				searchText.Text = searchText.Text.Slice(0, -2);
				DataGridHelper.Centrify(message.Sender as DataGrid);
			}
		}

		private static void AttachToCurrentCell(DataGrid grid, CompositeDisposable disposible, TextBox text)
		{
			var cell = DataGridHelper.GetCell(grid, grid.CurrentCell);
			if (cell == null)
				return;

			IDisposable lostFocusSubscription = null;
			lostFocusSubscription = Observable.FromEventPattern<RoutedEventArgs>(cell, "LostFocus")
				.Subscribe(_ => {
					text.Text = null;
					if (lostFocusSubscription != null) {
						disposible.Remove(lostFocusSubscription);
						lostFocusSubscription.Dispose();
					}
				});
			disposible.Add(lostFocusSubscription);
		}
	}
}