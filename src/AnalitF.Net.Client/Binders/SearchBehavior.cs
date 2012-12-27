using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Binders
{
	public class SearchBehavior
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
				if (Char.IsControl(args.Text[0]))
					return;
				searchText.Text += args.Text;
				DataGridHelper.Centrify(grid);
			};

			var disposable = new CompositeDisposable();

			searchText.IsVisibleChanged += (sender, args) => {
				if ((bool)args.NewValue) {
					var cellChangedSubscription = Observable
						.FromEventPattern<EventArgs>(grid, "CurrentCellChanged")
						.Subscribe(_ => AttachToCurrentCell(grid, disposable));

					disposable.Add(cellChangedSubscription);

					if (!((bool)grid.GetValue(Selector.IsSelectionActiveProperty)))
						DataGridHelper.Focus(grid);

					AttachToCurrentCell(grid, disposable);
				}
				else {
					disposable.Dispose();
					disposable = new CompositeDisposable();
				}
			};
		}

		private static void AttachToCurrentCell(DataGrid grid, CompositeDisposable disposible)
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
					var model = grid.DataContext as CatalogViewModel;
					if (model == null)
						return;
					model.SearchText = null;
					if (lostFocusSubscription != null) {
						disposible.Remove(lostFocusSubscription);
						lostFocusSubscription.Dispose();
					}
				});
			disposible.Add(lostFocusSubscription);
		}
	}
}