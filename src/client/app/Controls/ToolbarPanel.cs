using System;
using System.Windows;
using System.Windows.Controls;

namespace AnalitF.Net.Client.Controls
{
	//фактически это stackpanel но stackpanel не зажимает combobox а эта панель зажимает
	public class ToolbarPanel : Panel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			var size = new Size();
			foreach (UIElement child in InternalChildren) {
				child.Measure(availableSize);
				size.Width += child.DesiredSize.Width;
				size.Height = Math.Max(size.Height, child.DesiredSize.Height);
				availableSize.Width -= child.DesiredSize.Width;
				if (availableSize.Width < 0)
					availableSize.Width = 0;
			}
			return size;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var rect = new Rect(finalSize);
			var x = rect.X;
			var width = finalSize.Width;
			foreach (UIElement child in InternalChildren) {
				var childWidth = Math.Min(width, child.DesiredSize.Width);
				child.Arrange(new Rect(x, 0, childWidth, Math.Min(child.DesiredSize.Height, finalSize.Height)));
				x += childWidth;
				width -= childWidth;
			}
			return finalSize;
		}
	}
}