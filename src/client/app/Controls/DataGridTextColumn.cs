using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace AnalitF.Net.Client.Controls
{
	public class DataGridTextColumnEx : DataGridTextColumn
	{
		public static readonly DependencyProperty TextAlignmentProperty =
			Block.TextAlignmentProperty.AddOwner(typeof(DataGridTextColumnEx));

		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			if (Binding != null) {
				BindingOperations.SetBinding(cell, DataGridCell.ToolTipProperty, Binding);
			}
			var element = base.GenerateElement(cell, dataItem);
			SyncProperty(element, TextAlignmentProperty);
			return element;
		}

		private void SyncProperty(FrameworkElement element, DependencyProperty property)
		{
			var isDefault = DependencyPropertyHelper
				.GetValueSource(this, property)
				.BaseValueSource == BaseValueSource.Default;
			if (!isDefault) {
				element.SetValue(property, GetValue(property));
			}
		}

		public TextAlignment TextAlignment
		{
			get { return (TextAlignment)GetValue(TextAlignmentProperty); }
			set { SetValue(TextAlignmentProperty, value); }
		}


		protected override bool CommitCellEdit(FrameworkElement editingElement)
		{
			bool isValid;
			try {
				isValid = base.CommitCellEdit(editingElement);
			}
			catch(FormatException) {
				//analitf в случае невозможности преобразовать значение восстанавливает старое
				//реализуем аналогичное поведение
				//это единственный разумный способ реализации, все остальное порушит логику работы таблицы
				isValid = false;
			}

			if (isValid) {
				var exp = BindingOperations.GetBindingExpression(editingElement, TextBox.TextProperty);
				if (exp != null)
					exp.UpdateSource();
			}
			return isValid;
		}

		public string Name
		{
			get { return (string)GetValue(FrameworkElement.NameProperty); }
			set { SetValue(FrameworkElement.NameProperty, value); }
		}
	}
}