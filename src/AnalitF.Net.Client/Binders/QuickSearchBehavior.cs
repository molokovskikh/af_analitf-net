using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;

namespace AnalitF.Net.Client.Binders
{
	public class QuickSearchBehavior
	{
		//основная идея:
		//видимость поля ввода привязана к поисковому запросу
		//если запрос есть то поле видимо
		//если поле видимо то мы подписываеся на события
		//изменения ячейки и - что бы знать когда нужно изменить
		//подписку на потерю фокуса
		//потери фокуса ячейки - что бы знать что человек ушел из таблицы
		//и поиск нужно сбросить
		//если поле стало скрытым все подписки надо освободить
		public static void AttachSearch(DataGrid grid, TextBox searchText)
		{
			grid.TextInput += (sender, args) => {
				if (!searchText.IsEnabled)
					return;

				if (!Char.IsControl(args.Text[0])) {
					args.Handled = true;
					searchText.Text += args.Text;
					DataGridHelper.Centrify(grid);
				}
				else if (args.Text[0] == '\b') {
					args.Handled = true;
					searchText.Text = searchText.Text.Slice(0, -2);
					DataGridHelper.Centrify(grid);
				}
			};

			grid.KeyDown += (sender, args) => {
				if (searchText.IsEnabled)
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

		private static void AttachToCurrentCell(DataGrid grid, CompositeDisposable disposible, TextBox text)
		{
			var container = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.CurrentCell.Item);
			if (container == null)
				return;
			var cell = DataGridHelper.GetCell(container, grid.CurrentCell.Column);
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