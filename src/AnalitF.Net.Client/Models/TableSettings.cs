﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Binders;
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

			var isConventionApplied = (bool)dependencyObject.GetValue(ViewModelBinder.ConventionsAppliedProperty);
			if (isConventionApplied)
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
				.Where(c => (bool)c.GetValue(Persistable.PersistColumnSettingsProperty));
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
				var column = dataGrid.Columns.FirstOrDefault(c => c.Header.Equals(setting.Name));
				if (column == null)
					return;
				setting.Restore(column);
			}
		}

		public void Reset(DataGrid grid)
		{
			RestoreView(grid, defaults);
		}
	}
}