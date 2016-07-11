using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.Models.Print
{
	public class PriceTagDocument: BaseDocument
	{
		private IDictionary<string, object> properties;

		private Dictionary<string, Style> styles = new Dictionary<string, Style> {
			{"Product", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, 11d),
					new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
					new Setter(FrameworkElement.HeightProperty, 41d),
					new Setter(TextBlock.TextDecorationsProperty, TextDecorations.Underline),
					new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),
					new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"Cost", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, 18d),
					new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"Country", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, 10d),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"Producer", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, 10d),
					new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right),
				}
			}},
			{"Period", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, 9d),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"SerialNumber", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, 9d),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"ProviderDocumentId", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, 9d),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
		};

		private Waybill waybill;
		private Settings settings;
		private WaybillSettings waybillSettings;
		private IList<WaybillLine> lines;
		private PriceTag priceTag;

		public PriceTagDocument(Waybill waybill, IList<WaybillLine> lines, Settings settings, PriceTag priceTag)
		{
			this.waybill = waybill;
			this.waybillSettings = waybill.WaybillSettings;
			this.settings = settings;
			this.lines = lines;
			this.priceTag = priceTag;
		}

		protected override void BuildDoc()
		{
			properties = ObjectExtentions.ToDictionary(settings.PriceTag);
			Func<WaybillLine, FrameworkElement> map = Normal;

			if (settings.PriceTag.Type == PriceTagType.Small)
				map = Small;
			else if (settings.PriceTag.Type == PriceTagType.BigCost)
				map = Big;
			else if (settings.PriceTag.Type == PriceTagType.BigCost2)
				map = Big2;
			else if (settings.PriceTag.Type == PriceTagType.Custom)
				map = x => priceTag.ToElement(x);

			doc = FixedDocumentHelper.BuildFlowDoc(waybill, lines, waybillSettings, l => Border(map(l), 0.5), 0.5);
		}

		private string FormatCost(WaybillLine line)
		{
			if (settings.PriceTag.PrintEmpty)
				return "";

			var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
			format.NumberDecimalSeparator = "-";
			return String.Format(format, "{0:0.00}", line.RetailCost);
		}

		private static Border Border(FrameworkElement element, double borderThickness)
		{
			var border = new Border {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, borderThickness, borderThickness),
				Child = element,
			};
			return border;
		}

		private static void ApplyDefaults(DependencyObject canvas)
		{
			canvas.Descendants<TextBlock>()
				.Each(e => {
					e.Padding = new Thickness(2, 2, 0, 0);
					e.FontFamily = new FontFamily("Arial");
				});
			canvas.Descendants<Label>()
				.Each(e => {
					e.FontFamily = new FontFamily("Arial");
				});
		}

		private FrameworkElement Small(WaybillLine line)
		{
			var canvas = new Canvas {
				Height = 106,
				Width = 162,
			};
			var product = new TextBlock {
				Text = line.Product,
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				Height = 37,
				Width = 160
			};
			product.SetValue(Canvas.LeftProperty, 0d);
			product.SetValue(Canvas.TopProperty, 0d);
			canvas.Children.Add(product);

			var costLabel = new TextBlock {
				Text = "Цена",
				Width = 26,
				FontSize = 9
			};
			costLabel.SetValue(Canvas.LeftProperty, 0d);
			costLabel.SetValue(Canvas.TopProperty, 38d);
			canvas.Children.Add(costLabel);

			var cost = new TextBlock {
				Text = FormatCost(line),
				Width = 133,
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextAlignment = TextAlignment.Right,
			};
			cost.SetValue(Canvas.LeftProperty, 28d);
			cost.SetValue(Canvas.TopProperty, 38d);
			canvas.Children.Add(cost);

			var producerLabel = new TextBlock {
				Text = "Произв.",
				Width = 37,
				FontSize = 9
			};
			producerLabel.SetValue(Canvas.LeftProperty, 0d);
			producerLabel.SetValue(Canvas.TopProperty, 50d);
			canvas.Children.Add(producerLabel);
			var country = new TextBlock {
				Text = line.Country,
				Width = 122,
				FontSize = 9,
				TextAlignment = TextAlignment.Right,
			};
			country.SetValue(Canvas.LeftProperty, 39d);
			country.SetValue(Canvas.TopProperty, 50d);
			canvas.Children.Add(country);

			var producer = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = 9,
				Text = line.Producer,
				Width = 160,
			};
			producer.SetValue(Canvas.LeftProperty, 0d);
			producer.SetValue(Canvas.TopProperty, 62d);
			canvas.Children.Add(producer);

			var periodLabel = new TextBlock {
				Text = "Срок годности",
				Width = 65,
				FontSize = 9
			};
			periodLabel.SetValue(Canvas.LeftProperty, 0d);
			periodLabel.SetValue(Canvas.TopProperty, 73d);
			canvas.Children.Add(periodLabel);
			var period = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = 9,
				Text = line.Period,
				Width = 93,
			};
			period.SetValue(Canvas.LeftProperty, 67d);
			period.SetValue(Canvas.TopProperty, 73d);
			canvas.Children.Add(period);

			var singLabel = new TextBlock {
				Text = "Подпись",
				Width = 160,
				FontSize = 8
			};
			singLabel.SetValue(Canvas.LeftProperty, 0d);
			singLabel.SetValue(Canvas.TopProperty, 84d);
			canvas.Children.Add(singLabel);

			var waybillDate = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = 9,
				Text = line.Waybill.DocumentDate.ToShortDateString(),
				Width = 100,
			};
			waybillDate.SetValue(Canvas.LeftProperty, 46d);
			waybillDate.SetValue(Canvas.TopProperty, 84d);
			canvas.Children.Add(waybillDate);

			ApplyDefaults(canvas);

			return canvas;
		}

		public FrameworkElement Big(WaybillLine line)
		{
			var canvas = new Canvas {
				Width = 162,
				Height = 106,
			};
			var nameAndAddressLabel = new TextBlock {
				Text = waybillSettings.FullName,
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				FontSize = 8,
				Width = 162,
				Height = 20
			};
			nameAndAddressLabel.SetValue(Canvas.LeftProperty, 0d);
			nameAndAddressLabel.SetValue(Canvas.TopProperty, 0d);
			canvas.Children.Add(new Border {
				BorderThickness = new Thickness(0, 0, 0, 1),
				BorderBrush = Brushes.Black,
				Child = nameAndAddressLabel
			});

			var product = new TextBlock {
				Text = $"{line.Product}\n{line.Producer}",
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				Height = 37,
				Width = 160
			};
			product.SetValue(Canvas.LeftProperty, 0d);
			product.SetValue(Canvas.TopProperty, 21d);
			canvas.Children.Add(product);

			var cost = new TextBlock {
				Text = FormatCost(line),
				Width = 112,
				FontSize = 26,
				FontWeight = FontWeights.Bold,
				TextAlignment = TextAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			cost.SetValue(Canvas.LeftProperty, 49d);
			cost.SetValue(Canvas.TopProperty, 78d);
			canvas.Children.Add(cost);

			canvas.Add(0, 68, new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = 8,
				Text = "Годен до",
				Width = 49,
			});

			canvas.Add(0, 76, new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = 9,
				Text = line.Period,
				Width = 49,
			});

			canvas.Add(0, 86, new TextBlock {
				FontSize = 8,
				Text = "Подпись",
				Width = 49,
			});

			canvas.Add(0, 94, new TextBlock {
				FontSize = 9,
				Text = DateTime.Now.ToShortDateString(),
				Width = 49,
			});

			ApplyDefaults(canvas);

			return canvas;
		}

		public FrameworkElement Big2(WaybillLine line)
		{
			var canvas = new Canvas {
				Width = 162,
				Height = 106,
			};

			var nameAndAddressLabel = new TextBlock {
				Text = waybillSettings.FullName,
				TextAlignment = TextAlignment.Center,
				FontSize = 6,
				Width = 162,
				Height = 10
			};
			nameAndAddressLabel.SetValue(Canvas.LeftProperty, 0d);
			nameAndAddressLabel.SetValue(Canvas.TopProperty, 0d);
			canvas.Children.Add(new Border {
				BorderThickness = new Thickness(0, 0, 0, 1),
				BorderBrush = Brushes.Black,
				Child = nameAndAddressLabel
			});

			var serialNumberLabel = new TextBlock {
				Text = line.SerialNumber,
				TextAlignment = TextAlignment.Center,
				FontSize = 6,
				Width = 81,
				Height = 10
			};
			var serialNumberBorder = new Border {
				BorderThickness = new Thickness(0, 0, 1, 1),
				BorderBrush = Brushes.Black,
				Child = serialNumberLabel
			};
			serialNumberBorder.SetValue(Canvas.LeftProperty, 0d);
			serialNumberBorder.SetValue(Canvas.TopProperty, 10d);
			canvas.Children.Add(serialNumberBorder);

			var supplierNameLabel = new TextBlock {
				Text = line.Waybill.SupplierName,
				TextAlignment = TextAlignment.Center,
				FontSize = 6,
				Width = 81,
				Height = 10
			};
			var supplierNameBorder = new Border {
				BorderThickness = new Thickness(0, 0, 0, 1),
				BorderBrush = Brushes.Black,
				Child = supplierNameLabel
			};
			supplierNameBorder.SetValue(Canvas.LeftProperty, 81d);
			supplierNameBorder.SetValue(Canvas.TopProperty, 10d);
			canvas.Children.Add(supplierNameBorder);

			var product = new TextBlock {
				Text = $"{line.Product}\n{line.Producer}",
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				Height = 54,
				Width = 160
			};
			product.SetValue(Canvas.LeftProperty, 0d);
			product.SetValue(Canvas.TopProperty, 20d);
			canvas.Children.Add(product);

			var cost = new TextBlock {
				Text = FormatCost(line),
				Width = 112,
				FontSize = 26,
				FontWeight = FontWeights.Bold,
				TextAlignment = TextAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			cost.SetValue(Canvas.LeftProperty, 49d);
			cost.SetValue(Canvas.TopProperty, 78d);
			canvas.Children.Add(cost);

			var periodLabel = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = 8,
				Text = "Годен до",
				Width = 49,
			};
			periodLabel.SetValue(Canvas.LeftProperty, 0d);
			periodLabel.SetValue(Canvas.TopProperty, 78d);
			canvas.Children.Add(periodLabel);
			var period = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = 9,
				Text = line.Period,
				Width = 49,
			};
			period.SetValue(Canvas.LeftProperty, 0d);
			period.SetValue(Canvas.TopProperty, 86d);
			canvas.Children.Add(period);

			var singLabel = new TextBlock {
				FontSize = 8,
				Text = "Подпись",
				Width = 49,
			};
			singLabel.SetValue(Canvas.LeftProperty, 0d);
			singLabel.SetValue(Canvas.TopProperty, 96d);
			canvas.Children.Add(singLabel);

			ApplyDefaults(canvas);
			return canvas;
		}

		private FrameworkElement Normal(WaybillLine line)
		{
			var panel = new StackPanel {
				Width = 162,
				Margin = new Thickness(2)
			};
			if (!settings.PriceTag.HideNotPrinted || settings.PriceTag.PrintFullName) {
				var uri = new Uri(String.Format(@"pack://application:,,,/{0};component/assets/images/price-tag-log.png",
					typeof(PriceTagDocument).Assembly.GetName().Name));
				panel.Children.Add(new Border {
					BorderThickness = new Thickness(0, 0, 0, 0.5),
					BorderBrush = Brushes.Black,
					Child = new DockPanel {
						Width = 162,
						Children = {
							new Image {
								Height = 20,
								Width = 18,
								Source = new BitmapImage(uri)
							},
							new TextBlock {
								Height = 20,
								Text = settings.PriceTag.PrintFullName ? waybillSettings.FullName : "",
								FontSize = 8,
								TextAlignment = TextAlignment.Center,
								VerticalAlignment = VerticalAlignment.Center,
								TextWrapping = TextWrapping.Wrap
							}
						}
					}
				});
			}
			GetValue(panel, "", line.Product, "Product");
			GetValue(panel, "Цена", FormatCost(line), "Cost");
			GetValue(panel, "Произв.", line.Country, "Country");
			GetValue(panel, "", line.Producer, "Producer");
			GetValue(panel, "Срок годности", line.Period, "Period");
			GetValue(panel, "Серия товара", line.SerialNumber, "SerialNumber");
			GetValue(panel, "№ накладной", line.Waybill.ProviderDocumentId, "ProviderDocumentId");

			var haveValue = settings.PriceTag.PrintSupplier || settings.PriceTag.PrintDocumentDate;
			if (haveValue || !settings.PriceTag.HideNotPrinted) {
				var value = String.Format("{0:d}{1}",
					settings.PriceTag.PrintDocumentDate ? (DateTime?)line.Waybill.DocumentDate : null,
					settings.PriceTag.PrintSupplier ? line.Waybill.SupplierName : "");

				var label = new Label {
					FontSize = 8,
					Content = value,
					HorizontalContentAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(0),
					Padding = new Thickness(0)
				};
				var dockPanel = new DockPanel {
					Width = 162,
					Children = {
						new Label {
							VerticalAlignment = VerticalAlignment.Center,
							Content = "Подпись",
							FontSize = 8,
							Margin = new Thickness(0),
							Padding = new Thickness(0),
						},
						label
					}
				};
				label.SetValue(DockPanel.DockProperty, Dock.Right);
				panel.Children.Add(new Border {
					BorderThickness = new Thickness(0, 0.5, 0, 0),
					BorderBrush = Brushes.Black,
					Child = dockPanel
				});
			}

			ApplyDefaults(panel);

			return panel;
		}

		private void GetValue(StackPanel grid, string name, object value, string key)
		{
			var printKey = "Print" + key;
			var print = (bool)properties.GetValueOrDefault(printKey, true);

			if (!print) {
				if (settings.PriceTag.HideNotPrinted)
					return;

				value = "";
			}

			var label = new TextBlock {
				Style = styles.GetValueOrDefault(key),
				Text = (value ?? "").ToString(),
				Margin = new Thickness(0),
				Padding = new Thickness(0)
			};
			if (!String.IsNullOrEmpty(name)) {
				var dockPanel = new DockPanel {
					Width = 162,
					Children = {
						new Label {
							VerticalAlignment = VerticalAlignment.Center,
							Content = name,
							FontSize = 9,
							Margin = new Thickness(0),
							Padding = new Thickness(0),
						},
						label
					}
				};
				label.SetValue(DockPanel.DockProperty, Dock.Right);
				grid.Children.Add(dockPanel);
			}
			else {
				grid.Children.Add(label);
			}
		}
	}

	public static class TagHelper
	{
		public static void Add(this Canvas self, double left, double top, UIElement el)
		{
			el.SetValue(Canvas.LeftProperty, left);
			el.SetValue(Canvas.TopProperty, top);
			self.Children.Add(el);
		}
	}
}