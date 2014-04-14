using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Models
{
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

		private IEnumerable<DataGrid> GetControls(object view)
		{
			var dependencyObject = view as DependencyObject;
			if (dependencyObject == null)
				return Enumerable.Empty<DataGrid>();
			return dependencyObject.Descendants<DataGrid>()
				.Where(c => Interaction.GetBehaviors(c).OfType<Persistable>().Any());
		}

		private string GetViewKey(DataGrid grid)
		{
			return Prefix + grid.Name;
		}

		private void SaveView(DataGrid grid, Dictionary<string, List<ColumnSettings>> storage)
		{
			var key = GetViewKey(grid);
			if (storage.ContainsKey(key)) {
				storage.Remove(key);
			}
			storage.Add(key, grid.Columns.Select((c, i) => new ColumnSettings(c, i)).ToList());
		}

		private void RestoreView(DataGrid dataGrid, Dictionary<string, List<ColumnSettings>> storage)
		{
			var key = GetViewKey(dataGrid);
			if (!storage.ContainsKey(key))
				return;

			var settings = storage[key];
			if (settings == null)
				return;

			foreach (var setting in settings) {
				var column = DataGridHelper.GetColumn(dataGrid, setting.Name);
				setting.Restore(dataGrid.Columns, column);
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