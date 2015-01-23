using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class Editor : BaseNotify
	{
		private OrderLine lastEdit;
		private InlineEditWarning warning;
		private WindowManager manager;
		private NotifyValue<OrderLine> current;

		public Editor(InlineEditWarning warning, WindowManager manager, NotifyValue<OrderLine> current)
		{
			this.warning = warning;
			this.manager = manager;
			this.current = current;
		}

		public IList Lines { get; set; }

		public void ShowValidationError()
		{
			if (lastEdit == null)
				return;

			ShowValidationError(lastEdit.SaveValidate());
		}

		private void ShowValidationError(List<Message> messages)
		{
			warning.Show(messages);

			//если человек ушел с этой позиции а мы откатываем значение то нужно вернуть его к этой позиции что бы он
			//мог ввести корректное значение
			var errors = messages.Where(m => m.IsError);
			if (errors.Any()) {
				if (current.Value == null || current.Value.Id != lastEdit.Id) {
					current.Value = lastEdit;
				}
			}
		}

		public void Updated()
		{
			if (current.Value == null)
				return;

			lastEdit = current.Value;
			ShowValidationError(lastEdit.EditValidate());
			CheckForDelete(lastEdit);
		}

		public void Committed()
		{
			ShowValidationError();
		}

		private void CheckForDelete(OrderLine orderLine)
		{
			var order = orderLine.Order;
			if (orderLine.Count == 0) {
				lastEdit = null;
				if (order != null) {
					order.RemoveLine(orderLine);
					if (order.IsEmpty)
						order.Address.Orders.Remove(order);
				}
				Lines.Remove(orderLine);
			}

			if (order != null)
				order.UpdateStat();
		}

		public void Delete()
		{
			if (current.Value == null)
				return;

			if (manager.Question("Удалить позицию?") != MessageBoxResult.Yes)
				return;

			current.Value.Count = 0;
			CheckForDelete(current.Value);
		}
	}
}