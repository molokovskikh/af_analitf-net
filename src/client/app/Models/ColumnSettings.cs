using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace AnalitF.Net.Client.Models
{
	public class ColumnSettings
	{
		public ColumnSettings()
		{
		}

		public ColumnSettings(DataGridColumn dataGridColumn, int index)
		{
			Name = (dataGridColumn.Header ?? "").ToString();
			//видимостью колонки управляет флаг на форме
			//состояние не нужно сохранять
			if (Name != "Адрес заказа")
				Visible = dataGridColumn.Visibility;
			Width = dataGridColumn.Width;
			//-1 будет в том случае если таблица еще не отображалась
			//в этом случае надо использовать индекс колонки
			if (dataGridColumn.DisplayIndex != -1)
				DisplayIndex = dataGridColumn.DisplayIndex;
			else
				DisplayIndex = index;
		}

		public void Restore(ObservableCollection<DataGridColumn> columns, DataGridColumn column)
		{
			if (column == null)
				return;
			column.Width = Width;
			if (Name != "Адрес заказа")
				column.Visibility = Visible;
			//мы не можем установить неопределенный индекс или больше максимально индекса
			if (DisplayIndex >= 0 && DisplayIndex <= columns.Count - 1)
				column.DisplayIndex = DisplayIndex;
		}

		public string Name;
		public Visibility Visible;
		public DataGridLength Width;
		public int DisplayIndex;

		public override string ToString()
		{
			return Name;
		}

		public bool Match(DataGridColumn column)
		{
			return String.Equals(Name, (column.Header ?? "").ToString(), StringComparison.CurrentCultureIgnoreCase);
		}
	}
}