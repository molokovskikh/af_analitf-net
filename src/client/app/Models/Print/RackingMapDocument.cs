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

		public RackingMapDocument(Waybill waybill, IList<WaybillLine> lines, Settings settings)
		{
			this.settings = settings;
			this.waybill = waybill;
			this.waybillSettings = waybill.WaybillSettings;
			this.lines = lines;
		}

		public FixedDocument Build()
		{
			properties = ObjectExtentions.ToDictionary(settings.RackingMap);
			if (settings.RackingMap.Size == RackingMapSize.Normal2) {
				return FixedDocumentHelper.BuildFixedDoc(waybill, lines, waybillSettings, Map2, 1);
			}
			else if (settings.RackingMap.Size == RackingMapSize.Big) {
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
						new Setter(TextBlock.FontSizeProperty, 7d)
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
				FontSize = 11,
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

		private FrameworkElement Map2(WaybillLine line)
		{
			var body = new Grid {
				Height = 206,
				Width = 340,
				Margin = new Thickness(4, 0, 2, 4)
			};
			body.RowDefinitions.Add(new RowDefinition());
			body.RowDefinitions.Add(new RowDefinition());
			body.RowDefinitions.Add(new RowDefinition());
			body.RowDefinitions.Add(new RowDefinition());

			var header = new Grid();
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header.ColumnDefinitions.Add(new ColumnDefinition());
			header.ColumnDefinitions.Add(new ColumnDefinition());
			header.ColumnDefinitions.Add(new ColumnDefinition());

			var textBlock = new TextBlock {
				Text = "Поставка №:",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			};
			header.Cell(0, 0, textBlock);

			textBlock = new TextBlock {
				Text = "Накладная №:",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			};
			header.Cell(1, 0, textBlock);

			textBlock = new TextBlock {
				Text = line.Waybill.ProviderDocumentId,
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			};
			header.Cell(1, 1, textBlock);

			textBlock = new TextBlock {
				Text = "Поставщик:",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			};
			header.Cell(2, 0, textBlock);

			textBlock = new TextBlock {
				Text = line.Waybill.SupplierName,
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			};
			header.Children.Add(textBlock);
			textBlock.SetValue(Grid.RowProperty, 2);
			textBlock.SetValue(Grid.ColumnProperty, 1);
			textBlock.SetValue(Grid.ColumnSpanProperty, 2);

			textBlock = new TextBlock {
				Text = "Производитель:",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			};
			header.Cell(3, 0, textBlock);
			textBlock = new TextBlock {
				Text = line.Producer,
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			};
			header.Children.Add(textBlock);
			textBlock.SetValue(Grid.RowProperty, 3);
			textBlock.SetValue(Grid.ColumnProperty, 1);
			textBlock.SetValue(Grid.ColumnSpanProperty, 2);

			textBlock = new TextBlock {
				Text = line.Waybill.DocumentDate.ToShortDateString(),
				FontSize = 10,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontFamily = new FontFamily("Arial"),
			};
			header.Cell(0, 2, textBlock);
			textBlock = new TextBlock {
				Text = "(дата накладной)",
				FontSize = 9,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontFamily = new FontFamily("Arial"),
			};
			header.Cell(1, 2, textBlock);

			body.Children.Add(header);
			header.SetValue(Grid.RowProperty, 0);

			textBlock = new TextBlock {
				Text = line.Product,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				TextAlignment = TextAlignment.Center,
				FontSize = 13,
				FontFamily = new FontFamily("Arial"),
				FontWeight = FontWeights.Bold,
				Height = 46,
				Width = 332
			};
			body.Children.Add(textBlock);
			textBlock.SetValue(Grid.RowProperty, 1);

			var subBody = new Grid();
			subBody.RowDefinitions.Add(new RowDefinition {
				Height = GridLength.Auto
			});
			subBody.RowDefinitions.Add(new RowDefinition {
				Height = GridLength.Auto
			});
			subBody.ColumnDefinitions.Add(new ColumnDefinition());
			subBody.ColumnDefinitions.Add(new ColumnDefinition());
			subBody.ColumnDefinitions.Add(new ColumnDefinition());
			subBody.ColumnDefinitions.Add(new ColumnDefinition());

			subBody.Cell(0, 0, new TextBlock {
				Text = "Серия:",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			});
			subBody.Cell(0, 1, new TextBlock {
				Text = line.SerialNumber,
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			});
			subBody.Cell(0, 2, new TextBlock {
				Text = "Количество:",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			});
			subBody.Cell(0, 3, new TextBlock {
				Text = line.Quantity != null ? line.Quantity.ToString() : "",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			});
			subBody.Cell(1, 0, new TextBlock {
				Text = "Штрих-код:",
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			});
			subBody.Cell(1, 1, new TextBlock {
				Text = line.EAN13,
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
			});
			body.Children.Add(subBody);
			subBody.SetValue(Grid.RowProperty, 2);

			var markup = new Grid();
			markup.Margin = new Thickness(0, 0, 2, 0);
			markup.RowDefinitions.Add(new RowDefinition {
				Height = GridLength.Auto
			});
			markup.RowDefinitions.Add(new RowDefinition {
				Height = GridLength.Auto
			});
			markup.RowDefinitions.Add(new RowDefinition {
				Height = GridLength.Auto
			});
			markup.ColumnDefinitions.Add(new ColumnDefinition());
			markup.ColumnDefinitions.Add(new ColumnDefinition());
			markup.ColumnDefinitions.Add(new ColumnDefinition());
			markup.Cell(0, 1, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = "Нац. %",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1)
			});
			markup.Cell(0, 2, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = "Цена",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 1, 1, 1)
			});
			markup.Cell(1, 0, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = "Поставщик",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1)
			});
			markup.Cell(2, 0, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = "Аптека",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1, 0, 1, 1)
			});
			markup.Cell(1, 1, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = line.SupplierPriceMarkup != null ? line.SupplierPriceMarkup.Value.ToString("0.00") : "",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 1, 1),
			});
			markup.Cell(1, 2, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = line.SupplierCost != null ? line.SupplierCost.Value.ToString("0.00") : "",
				HorizontalContentAlignment = HorizontalAlignment.Right,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 1, 1),
			});
			markup.Cell(2, 1, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = line.RetailMarkup != null ? line.RetailMarkup.Value.ToString("0.00") : "",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 1, 1),
			});
			markup.Cell(2, 2, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = line.RetailCost != null ? line.RetailCost.Value.ToString("0.00") : "",
				HorizontalContentAlignment = HorizontalAlignment.Right,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 1, 1),
			});

			var tax = new Grid();
			tax.Margin = new Thickness(2, 0, 0, 0);
			tax.RowDefinitions.Add(new RowDefinition {
				Height = GridLength.Auto
			});
			tax.ColumnDefinitions.Add(new ColumnDefinition());
			tax.ColumnDefinitions.Add(new ColumnDefinition());
			tax.Cell(0, 0, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = String.Format("НДС {0:0.00} %", line.Nds),
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1, 1, 1, 1),
			});
			tax.Cell(0, 1, new Label {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Content = "НП __ %",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				Padding = new Thickness(0),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 1, 1, 1),
			});

			var period = new Grid();
			period.Margin = new Thickness(2, 0, 0, 0);
			tax.RowDefinitions.Add(new RowDefinition {
				Height = GridLength.Auto
			});
			period.ColumnDefinitions.Add(new ColumnDefinition());
			period.ColumnDefinitions.Add(new ColumnDefinition());
			period.Cell(0, 0, new TextBlock {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				HorizontalAlignment = HorizontalAlignment.Center,
				Text = "Срок годности:"
			});
			period.Cell(0, 1, new TextBlock {
				FontSize = 10,
				FontFamily = new FontFamily("Arial"),
				Text = line.Period
			});

			var footer = new Grid();
			footer.RowDefinitions.Add(new RowDefinition());
			footer.RowDefinitions.Add(new RowDefinition());
			footer.ColumnDefinitions.Add(new ColumnDefinition());
			footer.ColumnDefinitions.Add(new ColumnDefinition());
			footer.Children.Add(markup);
			markup.SetValue(Grid.RowProperty, 0);
			markup.SetValue(Grid.ColumnProperty, 0);
			markup.SetValue(Grid.RowSpanProperty, 2);
			footer.Cell(0, 1, tax);
			footer.Cell(1, 1, period);

			body.Children.Add(footer);
			footer.SetValue(Grid.RowProperty, 3);

			var border = new Border {
				BorderThickness = new Thickness(0, 0, 1, 1),
				BorderBrush = Brushes.Black,
			};
			border.Child = body;
			return border;
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
				FontSize = 10,
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
					FontSize = 9,
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

	public static class GridHelper
	{
		public static void Cell(this Grid subBody, int row, int col, UIElement el)
		{
			subBody.Children.Add(el);
			el.SetValue(Grid.RowProperty, row);
			el.SetValue(Grid.ColumnProperty, col);
		}
	}
}