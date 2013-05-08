using System;
using AnalitF.Net.Client.Helpers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	public class RackingMapDocument
	{
		private Size pageSize = new Size(816.0, 1056.0);

		private GridLength defaultHeight;
		private GridLength bigHeight;
		private GridLength labelColumnWidth;
		private GridLength valueColumnWidth;
		private int columnCount;
		private Dictionary<string, Style> styles = new Dictionary<string, Style> {
			{"Наименование ЛС", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),
					new Setter(TextBlock.FontWeightProperty, FontWeights.DemiBold)
				}
			}},
			{"Срок годности", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontWeightProperty, FontWeights.DemiBold),
				}
			}},
			{"Цена", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontWeightProperty, FontWeights.DemiBold)
				}
			}},
		};

		private Settings settings;

		public FixedDocument Build(Waybill waybill, WaybillSettings waybillSettings, Settings settings)
		{
			this.settings = settings;
			if (settings.RackingMap.Size == RackingMapSize.Big) {
				columnCount = 2;
				bigHeight = new GridLength(63);
				defaultHeight = new GridLength(21);
				labelColumnWidth = new GridLength(143);
				valueColumnWidth = new GridLength(147);
			}
			else {
				columnCount = 3;
				bigHeight = new GridLength(45);
				defaultHeight = new GridLength(15);
				labelColumnWidth = new GridLength(101);
				valueColumnWidth = new GridLength(117);
				styles.Add("Номер сертификата", new Style(typeof(TextBlock)) {
					Setters = {
						new Setter(TextBlock.FontSizeProperty, 5d)
					}
				});
			}

			var document = new FixedDocument();
			var start = 0;
			do {
				var panel = new StackPanel();
				var label = new Label {
					Padding = new Thickness(0, 5, 0, 5),
					Content = String.Format("{0} {1}", waybill.ProviderDocumentId, waybillSettings.FullName),
					FontFamily = new FontFamily("Arial"),
				};
				panel.Children.Add(label);
				panel.Children.Add(BuildMapGrid(waybill, pageSize, ref start));
				var page = new PageContent();
				page.Child = new FixedPage();
				var border = new Border {
					Margin = new Thickness(25),
					Child = panel,
				};
				page.Child.Children.Add(border);
				document.Pages.Add(page);
			} while (start < waybill.Lines.Count - 1);
			return document;
		}

		private FrameworkElement BuildMapGrid(Waybill waybill, Size size, ref int start)
		{
			var panel = new Grid();
			for (var i = 0; i < columnCount; i++)
				panel.ColumnDefinitions.Add(new ColumnDefinition());
			for (var i = 0; i < waybill.Lines.Count / columnCount; i ++)
				panel.RowDefinitions.Add(new RowDefinition());
			var border = new Border {
				Child = panel,
				BorderBrush = Brushes.Black,
				SnapsToDevicePixels = true,
				BorderThickness = new Thickness(2.5, 2.5, 0, 0)
			};

			var height = size.Height - border.BorderThickness.Top - border.BorderThickness.Bottom;

			for (var i = start; i < waybill.Lines.Count; i++) {
				start = i;
				var element = Map(waybill.Lines[i]);
				element.Measure(size);
				var columnIndex = i % columnCount;
				var rowIndex = i / columnCount;
				if (columnIndex == 0) {
					if (height < element.DesiredSize.Height) {
						break;
					}
					else {
						height -= element.DesiredSize.Height;
					}
				}
				element.SetValue(Grid.RowProperty, rowIndex);
				element.SetValue(Grid.ColumnProperty, columnIndex);
				panel.Children.Add(element);
			}
			return border;
		}

		private Grid Map(WaybillLine line)
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition { Height = defaultHeight });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = labelColumnWidth });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = valueColumnWidth });
			var header = new Label {
				Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Center,
				FontSize = 8,
				Content = "Стелажная карта",
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 2.5, 2.5)
			};
			header.SetValue(Grid.RowProperty, 0);
			header.SetValue(Grid.ColumnSpanProperty, 2);
			grid.Children.Add(header);

			var row = 1;
			Line(grid, "PrintProduct", "Наименование ЛС", line.Product, bigHeight, ref row);
			Line(grid, "PrintProducer", "Производитель", line.Producer, defaultHeight, ref row);
			Line(grid, "PrintSerialNumber", "Серия", line.SerialNumber, defaultHeight, ref row);
			Line(grid, "PrintPeriod", "Срок годности", line.Period, defaultHeight, ref row);
			Line(grid, "PrintQuantity", "Количество", line.Quantity.ToString(), defaultHeight, ref row);
			Line(grid, "PrintSupplier", "Поставщик", line.Waybill.Supplier != null ? line.Waybill.Supplier.FullName : "", defaultHeight, ref row);
			Line(grid, "PrintCertificates", "Номер сертификата", line.Certificates, defaultHeight, ref row);
			Line(grid, "PrintDocumentDate", "Дата поступления", line.Waybill.DocumentDate.ToString(), defaultHeight, ref row);
			Line(grid, "PrintRetailCost", "Цена", line.RetailCost.ToString(), defaultHeight, ref row);

			return grid;
		}

		private void Line(Grid grid, string key, string label, string value, GridLength rowHeight, ref int row)
		{
			var properties = ObjectExtentions.ToDictionary(settings.RackingMap);
			var printValue = (bool)properties[key];
			if (!printValue)
				value = "";

			if (!printValue && settings.RackingMap.HideNotPrinted)
				return;

			grid.RowDefinitions.Add(new RowDefinition { Height = rowHeight });
			var visualLabel = new Label {
				Margin = new Thickness(0),
				Padding = new Thickness(2, 0, 2, 0),
				Content = label,
				FontSize = 8,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 2.5, 2.5),
				FontFamily = new FontFamily("Arial"),
			};
			visualLabel.SetValue(Grid.RowProperty, row);
			visualLabel.SetValue(Grid.ColumnProperty, 0);
			grid.Children.Add(visualLabel);
			var visualValue = new Border {
				Child = new TextBlock {
					Text = value,
					FontSize = 7,
					Style = styles.GetValueOrDefault(label),
					SnapsToDevicePixels = true,
					FontFamily = new FontFamily("Arial"),
				},
				Padding = new Thickness(2, 0, 2, 0),
				BorderThickness = new Thickness(0, 0, 2.5, 2.5),
				BorderBrush = Brushes.Black,
			};
			visualValue.SetValue(Grid.RowProperty, row);
			visualValue.SetValue(Grid.ColumnProperty, 1);
			grid.Children.Add(visualValue);
			row++;
		}
	}
}