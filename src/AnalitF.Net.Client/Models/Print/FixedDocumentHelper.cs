using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	public class FixedDocumentHelper
	{
		private static Size pageSize = new Size(816.0, 1056.0);

		public static FixedDocument BuildFixedDoc(Waybill waybill, IList<WaybillLine> lines, WaybillSettings settings, Func<WaybillLine, FrameworkElement> map, double borderThickness)
		{
			var document = new FixedDocument();
			var start = 0;
			do {
				var panel = new StackPanel();
				var label = new Label {
					Padding = new Thickness(0, 5, 0, 5),
					Content = String.Format("{0} {1}", waybill.ProviderDocumentId, settings.FullName),
					FontFamily = new FontFamily("Arial"),
				};
				panel.Children.Add(label);
				var page = new PageContent();
				page.Child = new FixedPage();
				var border = new Border {
					Margin = new Thickness(25),
					Child = panel,
				};
				label.Measure(pageSize);
				var leftSize = new Size(pageSize.Width - border.Margin.Left - border.Margin.Right,
					pageSize.Height - border.DesiredSize.Height - border.Margin.Top - border.Margin.Bottom);
				panel.Children.Add(BuildMapGrid(i => map(lines[i]), lines.Count, leftSize, ref start, borderThickness));
				page.Child.Children.Add(border);
				document.Pages.Add(page);
			} while (start < lines.Count - 1);
			return document;
		}

		public static FrameworkElement BuildMapGrid(Func<int, FrameworkElement> map, int count, Size size, ref int start, double borderThickness)
		{
			var panel = new Grid();
			var border = new Border {
				Child = panel,
				BorderBrush = Brushes.Black,
				SnapsToDevicePixels = true,
				BorderThickness = new Thickness(borderThickness, borderThickness, 0, 0)
			};

			var height = size.Height - border.BorderThickness.Top - border.BorderThickness.Bottom;
			var width = size.Width - border.BorderThickness.Left - border.BorderThickness.Right;

			var columnIndex = 0;
			var rowIndex = 0;
			double consumedWidth = 0;
			for (var i = start; i < count; i++) {
				start = i;
				var element = map(i);
				element.Measure(size);

				if (consumedWidth + element.DesiredSize.Width > width) {
					consumedWidth = 0;
					columnIndex = 0;
					rowIndex++;
				}
				consumedWidth += element.DesiredSize.Width;

				if (columnIndex == 0) {
					if (height < element.DesiredSize.Height) {
						break;
					}
					else {
						height -= element.DesiredSize.Height;
					}
				}
				if (rowIndex > panel.RowDefinitions.Count - 1)
					panel.RowDefinitions.Add(new RowDefinition());
				if (columnIndex > panel.ColumnDefinitions.Count - 1)
					panel.ColumnDefinitions.Add(new ColumnDefinition());

				element.SetValue(Grid.RowProperty, rowIndex);
				element.SetValue(Grid.ColumnProperty, columnIndex);
				panel.Children.Add(element);
				columnIndex++;
			}
			return border;
		}
	}
}