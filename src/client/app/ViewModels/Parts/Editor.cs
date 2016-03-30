using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public interface IEditor
	{
		void Updated();
		void Committed();
	}

	public class Editor : BaseNotify, IEditor
	{
		private OrderLine lastEdit;
		private InlineEditWarning warning;
		private WindowManager manager;
		private NotifyValue<OrderLine> current;
		private uint lastEditCountCandidate;
		private uint lastValidCount;
		private NotifyValue<IList> lines;

		public Editor(InlineEditWarning warning, WindowManager manager,
			NotifyValue<OrderLine> current,
			NotifyValue<IList> lines)
		{
			this.warning = warning;
			this.manager = manager;
			this.current = current;
			this.lines = lines;
			current.Subscribe(x => lastEditCountCandidate = x?.Count ?? 0);
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
			lastValidCount = lastEditCountCandidate;
			if (current.Value == null)
				return;

			lastEdit = current.Value;
			ShowValidationError(lastEdit.EditValidate());
			CheckForDelete(lastEdit);
		}

		public void Committed()
		{
			if (lastEdit == null)
				return;

			ShowValidationError(lastEdit.SaveValidate(lastValidCount));
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
				lines.Value?.Remove(orderLine);
			}

			order?.UpdateStat();
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