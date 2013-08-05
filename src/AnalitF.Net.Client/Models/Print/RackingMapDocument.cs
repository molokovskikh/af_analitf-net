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
		private GridLength defaultHeight;
		private GridLength bigHeight;
		private GridLength labelColumnWidth;
		private GridLength valueColumnWidth;
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
		private WaybillSettings waybillSettings;
		private Waybill waybill;
		private IList<WaybillLine> lines;
		private IDictionary<string, object> properties;

		public RackingMapDocument(Waybill waybill, IList<WaybillLine> lines, Settings settings, WaybillSettings waybillSettings)
		{
			this.settings = settings;
			this.waybillSettings = waybillSettings;
			this.waybill = waybill;
			this.lines = lines;
		}

		public FixedDocument Build()
		{
			properties = ObjectExtentions.ToDictionary(settings.RackingMap);
			if (settings.RackingMap.Size == RackingMapSize.Big) {
				bigHeight = new GridLength(63);
				defaultHeight = new GridLength(21);
				labelColumnWidth = new GridLength(143);
				valueColumnWidth = new GridLength(147);
			}
			else {
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

			return FixedDocumentHelper.BuildFixedDoc(waybill, lines, waybillSettings, Map, 2.5);
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
				Content = "Стеллажная карта",
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 2.5, 2.5),
				FontFamily = new FontFamily("Arial"),
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
			Line(grid, "PrintSupplier", "Поставщик", line.Waybill.SupplierName, defaultHeight, ref row);
			Line(grid, "PrintCertificates", "Номер сертификата", line.Certificates, defaultHeight, ref row);
			Line(grid, "PrintDocumentDate", "Дата поступления", line.Waybill.DocumentDate.ToString(), defaultHeight, ref row);
			Line(grid, "PrintRetailCost", "Цена", line.RetailCost.ToString(), defaultHeight, ref row);

			return grid;
		}

		private void Line(Grid grid, string key, string label, string value, GridLength rowHeight, ref int row)
		{
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