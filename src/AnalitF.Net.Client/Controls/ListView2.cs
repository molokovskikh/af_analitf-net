using System;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace AnalitF.Net.Client.Controls
{
	public class ListView2 : ListView
	{
		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);

			//если строка удаляет с помощью функции datagrid то после удалени фокус остается в datagrid
			//если же удаление производится из коллекции ItemsSource то CurrentItem сбрасывается в null
			//и таблица теряет фокус
			if (e.Action == NotifyCollectionChangedAction.Remove) {
				var index = Math.Min(e.OldStartingIndex, Items.Count - 1);
				if (index >= 0) {
					SelectedItem = Items[index];
					//фокус должен быть на выбранном элементе
					//если после удаления мы его потеряли то его надо вернуть
					if (IsFocused) {
						ScrollIntoView(SelectedItem);
						var container = ItemContainerGenerator.ContainerFromItem(SelectedItem) as ListViewItem;
						if (container != null)
							container.Focus();
					}
				}
			}
		}
	}
}