using System;
using System.Collections;
using System.Reflection;
using System.Windows.Controls;

namespace AnalitF.Net.Client.Controls
{
	public class ComboBox2 : ComboBox
	{
		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			//баг в combobox
			//проявляется если открыть список -> ввести текст для поиска -> выбрать позицию стрелкой вниз
			//очистить выбранную позицию backspace -> открыть список -> ввести новый текст -> попытка выбрать позицию стрелкой не даст результатов
			//суть ошибки в том что при нажатии стрелки вниз фактически выпадающий список пытается выбрать следующую за выбранной позицией
			//выбранная позиция хранится в поле HighlightedInfo, но при изменении источника данных поле не очищается
			//те при повторном поиске мы пытаемся выбрать следующую позицию за текущей что не даст результатов тк текущая позиция
			//не будет найдена в списке элементов
			var propertyInfo = typeof(ComboBox).GetProperty("HighlightedInfo", BindingFlags.NonPublic | BindingFlags.Instance);
			if (propertyInfo != null)
				propertyInfo.SetValue(this, null, null);
		}
	}
}