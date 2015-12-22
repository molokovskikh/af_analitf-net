using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AnalitF.Net.Client.Controls
{
	public class LegendPanel : Panel
	{
		public static DependencyProperty IsCollapsedProperty = DependencyProperty.Register("IsCollapsed",
			typeof(bool), typeof(LegendPanel), new FrameworkPropertyMetadata(true,
				FrameworkPropertyMetadataOptions.AffectsMeasure));

		public static DependencyProperty IsOverflowProperty = DependencyProperty.Register("IsOverflow",
			typeof(bool), typeof(LegendPanel), new FrameworkPropertyMetadata(false));

		private double requiredWidth;

		public bool IsOverflow
		{
			get { return (bool)GetValue(IsOverflowProperty); }
			set { SetValue(IsOverflowProperty, value); }
		}

		public bool IsCollapsed
		{
			get { return (bool)GetValue(IsCollapsedProperty); }
			set { SetValue(IsCollapsedProperty, value); }
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (IsCollapsed) {
				return MesureSingleLine(availableSize);
			} else {
				return MesureMultiLine(availableSize);
			}
		}

		private Size MesureMultiLine(Size availableSize)
		{
			var size = new Size();
			double rowHeight = 0;
			double rowWidth = 0;
			var availableWidth = availableSize.Width;
			foreach (UIElement child in InternalChildren) {
				child.Measure(availableSize);
				rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
				if (child.DesiredSize.Width > availableWidth && availableWidth < availableSize.Width) {
					availableWidth = availableSize.Width;
					rowWidth = 0;
					rowHeight = child.DesiredSize.Height;
					size.Width = Math.Max(size.Width, rowWidth);
					size.Height += rowHeight;
				}
				rowWidth += child.DesiredSize.Width;
				availableWidth -= child.DesiredSize.Width;
			}
			size.Height += rowHeight;
			return size;
		}

		private Size MesureSingleLine(Size availableSize)
		{
			var size = new Size();
			requiredWidth = 0d;
			foreach (UIElement child in InternalChildren) {
				//для вычисления флага переполнения
				child.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
				requiredWidth += child.DesiredSize.Width;

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
			if (IsCollapsed)
				return ArrangeSingleLine(finalSize);
			else
				return ArrageMultiLine(finalSize);
		}

		private Size ArrageMultiLine(Size finalSize)
		{
			var x = 0d;
			var y = 0d;
			var width = finalSize.Width;
			var height = finalSize.Height;
			foreach (UIElement child in InternalChildren) {
				if (child.DesiredSize.Width > width && width < finalSize.Width) {
					width = finalSize.Width;
					height -= child.DesiredSize.Height;
					y += child.DesiredSize.Height;
					x = 0;
				}
				var childWidth = Math.Min(child.DesiredSize.Width, width);
				var childHeight = Math.Min(child.DesiredSize.Height, height);
				child.Arrange(new Rect(x, y, childWidth, childHeight));
				x += childWidth;
				width -= childWidth;
			}
			return finalSize;
		}

		private Size ArrangeSingleLine(Size finalSize)
		{
			IsOverflow = requiredWidth > finalSize.Width;

			var x = 0d;
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