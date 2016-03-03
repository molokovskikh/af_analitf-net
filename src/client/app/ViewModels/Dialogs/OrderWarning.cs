using System;
using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class OrderWarning : TextViewModel, ICancelable
	{
		public OrderWarning(List<Order> orders)
		{
			Header = "Данные заказы могут быть отправлены по согласованию с Поставщиком\n" +
				"однако сумма заказа меньше минимальной допустимой поставщиком:";
			DisplayName = "Ошибка отправки заказов";

			Text = orders.Implode(
				o => $"прайс-лист {o.Price.Name} - минимальный заказа {o.MinOrderSum.MinOrderSum:C} - заказано {o.Sum:C}",
				Environment.NewLine);
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}

		public override string ToString()
		{
			return Text;
		}
	}
}