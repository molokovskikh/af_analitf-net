using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq.Observαble;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Models
{
	public class ColumnSettings : BaseNotify
	{
		[JsonIgnore]
		private int _pixelWidth;

		[JsonIgnore]
		private int _displayIndex;

		[JsonIgnore]
		public bool IsDirty;

		public ColumnSettings()
		{
		}

		public ColumnSettings(DataGrid grid, DataGridColumn column, int index)
		{
			_pixelWidth = (int)column.ActualWidth;
			Name = DataGridHelper.GetHeader(column);
			//видимостью колонки управляет флаг на форме
			//состояние не нужно сохранять
			if (Name != "Адрес заказа") 
				Visible = column.Visibility;
			if (column.Width.IsStar)
				Width = column.Width;
			else
				Width = column.ActualWidth;
			//-1 будет в том случае если таблица еще не отображалась
			//в этом случае надо использовать индекс колонки
			if (column.DisplayIndex != -1)
				DisplayIndex = column.DisplayIndex;
			else
				DisplayIndex = index;
			if (grid.CanUserSortColumns && column.CanUserSort) {
				SortDirection = column.SortDirection;
			}
		}

		public void Restore(DataGrid grid, ObservableCollection<DataGridColumn> columns)
		{
			var column = DataGridHelper.FindColumn(columns, Name);
			if (column == null)
				return;
			column.Width = Width;
			if (Name != "Адрес заказа") 
				column.Visibility = Visible;
			//мы не можем установить неопределенный индекс или больше максимально индекса
			if (DisplayIndex >= 0 && DisplayIndex <= columns.Count - 1)
				column.DisplayIndex = DisplayIndex;
		}

		public string Name { get; set; }
		public Visibility Visible { get; set; }
		public ListSortDirection? SortDirection;

		[JsonIgnore]
		public bool IsVisible
		{
			get { return Visible == Visibility.Visible; }
			set
			{
				var result = value ? Visibility.Visible : Visibility.Collapsed;
				if (Visible == result)
					return;

				Visible = result;
				IsDirty = true;
				OnPropertyChanged();
			}
		}

		[JsonIgnore]
		public int PixelWidth
		{
			get { return _pixelWidth; }
			set
			{
				if (value == _pixelWidth)
					return;
				_pixelWidth = value;
				IsDirty = true;
			}
		}

		public DataGridLength Width { get; set; }

		public int DisplayIndex
		{
			get
			{
				return _displayIndex;
			}
			set
			{
				if (_displayIndex == value)
					return;

				_displayIndex = value;
				IsDirty = true;
				OnPropertyChanged();
			}
		}

		public override string ToString()
		{
			return Name;
		}
	}
}