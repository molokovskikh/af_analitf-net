using System.Collections.Generic;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class HelpItem
	{
		public HelpItem(string hotKeys, string name)
		{
			HotKeys = hotKeys;
			Name = name;
		}

		public string HotKeys { get; set; }
		public string Name { get; set; }
	}

	public class Help : Screen
	{
		public Help()
		{
			Items = new List<HelpItem> {
				new HelpItem("F1", "Вызов справки"),
				new HelpItem("F2", "Поиск по коду"),
				new HelpItem("F3", "Поиск по штрих-коду"),
				new HelpItem("F4", "Оплата/Возврат"),
				new HelpItem("F5", "Сменить тип оплаты"),
				new HelpItem("F6", "Поиск товара по наименованию"),
				new HelpItem("F7", "Поиск товара по цене"),
				new HelpItem("Esc", "Очистить поле количество"),
				new HelpItem("Enter", "Закрыть"),
				new HelpItem("* (NUM)", "Перенести содержимое поля ввода в поле количество"),
				new HelpItem("Alt + Delete", "Отменить чек"),
				new HelpItem("Ctrl + Q", "Редактирование количества"),
				new HelpItem("Ctrl + R", "Распаковка"),
			};
			DisplayName = "Справка";
		}

		public List<HelpItem> Items { get; set; }
	}
}