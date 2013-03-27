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
			Name = dataGridColumn.Header.ToString();
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

		public void Restore(DataGridColumn column)
		{
			column.Width = Width;
			column.Visibility = Visible;
			//мы не можем установить неопределенный индекс
			if (DisplayIndex != -1)
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
	}
}