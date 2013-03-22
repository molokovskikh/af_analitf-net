using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class Editor : INotifyPropertyChanged
	{
		private OrderLine lastEdit;
		private InlineEditWarning warning;
		private WindowManager manager;
		private OrderLine currentLine;

		public Editor(InlineEditWarning warning, WindowManager manager)
		{
			this.warning = warning;
			this.manager = manager;
		}

		public OrderLine CurrentEdit
		{
			get { return currentLine; }
			set
			{
				if (currentLine == value)
					return;

				currentLine = value;
				OnPropertyChanged("CurrentLine");
			}
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
				if (CurrentEdit == null || CurrentEdit.Id != lastEdit.Id) {
					CurrentEdit = lastEdit;
				}
			}
		}

		public void Updated()
		{
			if (CurrentEdit == null)
				return;

			lastEdit = CurrentEdit;
			ShowValidationError(lastEdit.EditValidate());
			CheckForDelete(lastEdit);
		}

		public void Committed()
		{
			ShowValidationError();
		}

		private void CheckForDelete(OrderLine orderLine)
		{
			if (orderLine.Count == 0) {
				lastEdit = null;
				var order = orderLine.Order;
				if (order != null) {
					order.RemoveLine(orderLine);
					if (order.IsEmpty)
						order.Address.Orders.Remove(order);
				}
				Lines.Remove(orderLine);
			}

			if (orderLine.Order != null) {
				orderLine.Order.Sum = orderLine.Order.Lines.Sum(l => l.Sum);
			}
		}

		public void Delete()
		{
			if (CurrentEdit == null)
				return;

			if (manager.Question("Удалить позицию?") != MessageBoxResult.Yes)
				return;

			CurrentEdit.Count = 0;
			CheckForDelete(CurrentEdit);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}