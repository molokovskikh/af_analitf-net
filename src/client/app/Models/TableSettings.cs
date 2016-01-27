using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	//todo после изменений таблицы начали терять фокус
	public class TableSettings
	{
		private Dictionary<string, List<ColumnSettings>> defaults
			= new Dictionary<string, List<ColumnSettings>>();

		public Dictionary<string, List<ColumnSettings>> Persisted
			= new Dictionary<string, List<ColumnSettings>>();
		public string Prefix = "";

		public void SaveView(object view)
		{
			if (view == null)
				return;

			foreach (var grid in GetControls(view)) {
				SaveView(grid, Persisted);
			}
		}

		public void RestoreView(object view)
		{
			//метод может быть вызван множество раз
			//сохранять значения по умолчанию и восстанавливать сохраненное нужно только
			//в первый раз
			var dependencyObject = view as DependencyObject;
			if (dependencyObject == null)
				return;

			if (defaults.Count != 0)
				return;

			foreach (var grid in GetControls(view)) {
				SaveView(grid, defaults);
				RestoreView(grid, Persisted);
			}
		}

		public void Restore(params DataGrid[] grids)
		{
			foreach (var grid in grids) {
				SaveView(grid, defaults);
				RestoreView(grid, Persisted);
			}
		}

		private IEnumerable<DataGrid> GetControls(object view)
		{
			var dependencyObject = view as DependencyObject;
			if (dependencyObject == null)
				return Enumerable.Empty<DataGrid>();
			return dependencyObject.LogicalDescendants().OfType<DataGrid>()
				.Where(c => Interaction.GetBehaviors(c).OfType<Persistable>().Any());
		}

		private string GetViewKey(FrameworkElement grid)
		{
			return Prefix + grid.Name;
		}

		private void SaveView(DataGrid grid, Dictionary<string, List<ColumnSettings>> storage)
		{
			var key = GetViewKey(grid);
			if (storage.ContainsKey(key)) {
				storage.Remove(key);
			}
			storage.Add(key, grid.Columns.Select((c, i) => new ColumnSettings(grid, c, i)).ToList());
		}

		private void RestoreView(DataGrid dataGrid, Dictionary<string, List<ColumnSettings>> storage)
		{
			var settings = storage.GetValueOrDefault(GetViewKey(dataGrid));
			if (settings == null)
				return;

			//набор колонок для которых производится восстановление и сохраненный набор колонок
			//могут отличаться тк ui может захотеть удалить какую нибудь колонку
			//в этом случае
			//в начале с0 с1 с2 с3 ui удалил колонку с1 с2 с3 пользователь поменял местами с2 с1 с3
			//получим сохраненные значения с1-1 с2-0 с3-2
			//восстановление с0 с1 с2 с3
			//шаг1 (с1 и так 1 ничего не поменялось) - с0 с1 с2 с3
			//шаг2 (у с2 0 ставим его вперед все сдвигаем)- с2 с0 с1 с3
			//шаг3 (у с3 2 ставим на место с1, с1 сдвигаем)- с2 с0 с3 с1
			//после того как ui удалит колонку получится с2 с3 с1 те с3 и с1 поменялись местами что не верно
			//если колонки обновлять в порядки отображения то слева от текущей позиции будут младшие колонки в правильном
			//порядке а справа старшие но порядок будет неправильный и отображаться будет верный порядок вне зависимости
			//от удаления колонок
			foreach (var column in settings.OrderBy(c => c.DisplayIndex))
				column.Restore(dataGrid, dataGrid.Columns);

			//восстанавливаем порядок сортировки
			//нужно сбросить маркер для всех колонок в ручную тк
			//dataGrid.Items.SortDescriptions.Clear(); делает это только для колонок которые отсортированы с
			//помощью SortDescriptions если сортировка по умолчанию делается при выборке а колонке просто назначается маркер
			//сортировка не будет сброшена
			var sorted = settings.FirstOrDefault(x => x.SortDirection != null);
			if (sorted != null) {
				var column = DataGridHelper.FindColumn(dataGrid, sorted.Name);
				if (column != null) {
					foreach (var gridColumn in dataGrid.Columns)
						gridColumn.SortDirection = null;
					column.SortDirection = sorted.SortDirection;
					dataGrid.Items.SortDescriptions.Clear();
					dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, column.SortDirection.Value));
				}
			}

			//тк новые колонки не имеют сохраненных настроек
			//они окажутся в конце таблицы
			//назначаем им индексы из значений по умолчанию
			foreach (var column in dataGrid.Columns.Where(c => !settings.Select(s => s.Name).Contains(DataGridHelper.GetHeader(c)))) {
				column.DisplayIndex = dataGrid.Columns.IndexOf(column);
			}
		}

		public void Reset(DataGrid grid)
		{
			RestoreView(grid, defaults);
		}
	}
}