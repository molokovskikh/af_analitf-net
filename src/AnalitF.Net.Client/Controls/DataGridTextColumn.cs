using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AnalitF.Net.Client.Controls
{
	public class DataGridTextColumnEx : DataGridTextColumn
	{
		public static readonly DependencyProperty TextAlignmentProperty =
			Block.TextAlignmentProperty.AddOwner(typeof(DataGridTextColumnEx));

		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
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
	}
}