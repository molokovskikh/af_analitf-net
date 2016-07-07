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
			var left = lines.Count;
			while (left > 0) {
				var panel = new StackPanel();
				var label = new Label {
					Padding = new Thickness(0, 5, 0, 5),
					Content = $"{waybill.ProviderDocumentId} {settings.FullName}",
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
				panel.Children.Add(BuildMapGrid(i => map(lines[i]), lines.Count, leftSize, ref left, borderThickness));
				page.Child.Children.Add(border);
				document.Pages.Add(page);
			}
			return document;
		}

		public static FlowDocument BuildFlowDoc(Waybill waybill, IList<WaybillLine> lines, WaybillSettings settings, Func<WaybillLine, FrameworkElement> map, double borderThickness)
		{
			var document = new FlowDocument();
			var left = lines.Count;
			while (left > 0) {
				var panel = new StackPanel();
				var label = new Label {
					Padding = new Thickness(0, 5, 0, 5),
					Content = $"{waybill.ProviderDocumentId} {settings.FullName}",
					FontFamily = new FontFamily("Arial"),
				};
				panel.Children.Add(label);
				var border = new Border {
					Margin = new Thickness(35)
				};
				var section = new Section();
				section.BreakPageBefore = true;
				label.Measure(pageSize);
				var leftSize = new Size(pageSize.Width - border.Margin.Left - border.Margin.Right,
					pageSize.Height - border.DesiredSize.Height - border.Margin.Top - border.Margin.Bottom);
				var grid = new BlockUIContainer(BuildMapGrid(i => map(lines[i]), lines.Count, leftSize, ref left, borderThickness));
				section.Blocks.Add(new BlockUIContainer(panel));
				section.Blocks.Add(grid);
				document.Blocks.Add(section);
			}
			return document;
		}

		public static FrameworkElement BuildMapGrid(Func<int, FrameworkElement> map, int count, Size size, ref int left, double borderThickness)
		{
			var panel = new Grid();
			var border = new Border {
				Child = panel,
				BorderBrush = Brushes.Black,
				SnapsToDevicePixels = true,
				BorderThickness = new Thickness(borderThickness, borderThickness, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left
			};

			var width = size.Width - border.BorderThickness.Left - border.BorderThickness.Right;
			var height = size.Height - border.BorderThickness.Top - border.BorderThickness.Bottom;
			var resultSize = new Size(width, height);
			var leftLocal = left;
			var offset = count - left;
			MakeGrid(resultSize, left, i => {
					var element = map(offset + i);
					element.Measure(resultSize);
				return element.DesiredSize;
				}, (i, r, c) => {
					leftLocal--;
					var element = map(offset + i);
					element.Measure(resultSize);
					if (r > panel.RowDefinitions.Count - 1)
						panel.RowDefinitions.Add(new RowDefinition());
					if (c > panel.ColumnDefinitions.Count - 1)
						panel.ColumnDefinitions.Add(new ColumnDefinition());

					element.SetValue(Grid.RowProperty, r);
					element.SetValue(Grid.ColumnProperty, c);
					panel.Children.Add(element);
				});
			left = leftLocal;
			return border;
		}

		public static void MakeGrid(Size size, int count, Func<int, Size> mesure, Action<int, int, int> place)
		{
			var index = 0;
			var col = 0;
			var row = 0;
			var width = size.Width;
			var height = size.Height;
			var leftHeight = height;
			while (leftHeight > 0) {
				var leftWidth = width;
				var rowHeight = 0d;
				while (leftWidth > 0) {
					var cellSize = mesure(index);
					rowHeight = Math.Max(rowHeight, cellSize.Height);
					//если есть достаточно место формируем ячейку
					//если ячейка первая но места нет значит размер ячейки больше размера страницы и мы все равно должны его ее сформировать
					if ((cellSize.Height <= leftHeight || row == 0) && (cellSize.Width <= leftWidth || col == 0)) {
						place(index, row, col);
						col++;
						leftWidth -= cellSize.Width;
						index++;
						if (index == count)
							return;
					} else {
						break;
					}
				}
				leftHeight -= rowHeight;
				col = 0;
				row++;
			}
		}
	}
}