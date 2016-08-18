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
			//, out List<Size> contentSize)
			//contentSize = new List<Size>();
			var canvasSize = new Size(10000, 10000);
			var document = new FlowDocument();
			document.PagePadding = new Thickness(0,0,0,0);
			var left = lines.Count;
			while (left > 0) {

				var section = new Section();
				section.BreakPageBefore = true;

				var leftSize = new Size(pageSize.Width,pageSize.Height);

				var footerElement = new Border {
					Child = new TextBlock(new Run($"{waybill.SupplierName}, {waybill.AddressName}"))
				};
				var footer = new BlockUIContainer(footerElement);
				var gridElement = BuildMapGrid(i => map(lines[i]), lines.Count, leftSize, ref left, borderThickness);
				var grid = new BlockUIContainer(gridElement);

				footerElement.Measure(canvasSize);
				gridElement.Measure(canvasSize);
				double currWidth = gridElement.DesiredSize.Width;
				if (currWidth < footerElement.DesiredSize.Width)
					currWidth = pageSize.Width;
				var currPageSize = new Size(currWidth, footerElement.DesiredSize.Height + gridElement.DesiredSize.Height);
				//contentSize.Add(currPageSize);

				document.MinPageWidth = currPageSize.Width;
				document.MinPageHeight = currPageSize.Height;

				document.Blocks.Add(section);

				var table = new Table {
					FontFamily = new FontFamily("Arial"),
					FontSize = 10,
					Columns = {
						new TableColumn {
							Width = GridLength.Auto
						}
					},
					RowGroups = {
						new TableRowGroup {
							Rows = {
								new TableRow {
									Cells = {
										new TableCell(footer) {
											TextAlignment = TextAlignment.Left,
										}
									}
								},
								new TableRow {
									Cells = {
										new TableCell(grid) {
											TextAlignment = TextAlignment.Left,
										}
									}
								}
							}
						}
					}
				};
				section.Blocks.Add(table);
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