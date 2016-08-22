using System;
using Common.Tools;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Frontend : BaseScreen2
	{
		public Frontend()
		{
			DisplayName = "Регистрация продаж";
		}

		public NotifyValue<string> Input { get; set; }
		public NotifyValue<string> LastOperation { get; set; }

		public void UpdateQuantity()
		{
			NullableConvert.
		}
	}
}