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
				new HelpItem("F3", "Очистить"),
				new HelpItem("F4", "Возврат"),
				new HelpItem("F7", "Закрыть чек")
			};
			DisplayName = "Справка";
		}

		public List<HelpItem> Items { get; set; }
	}
}