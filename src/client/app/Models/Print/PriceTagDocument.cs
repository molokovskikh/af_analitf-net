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

		static double pts(double val)
		{
			return val * 1.224d;
		}

		private Dictionary<string, Style> styles = new Dictionary<string, Style> {
			{"Product", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, pts(11d)),
					new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
					new Setter(FrameworkElement.HeightProperty, pts(41d)),
					new Setter(TextBlock.TextDecorationsProperty, TextDecorations.Underline),
					new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),
					new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center),
				}
			}},
			{"Cost", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, pts(22d)),
					new Setter(TextBlock.FontWeightProperty, FontWeights.Bold),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"Country", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, pts(11d)),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"Producer", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, pts(11d)),
					new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right),
				}
			}},
			{"Period", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, pts(10d)),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"SerialNumber", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, pts(10d)),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
			{"ProviderDocumentId", new Style(typeof(TextBlock)) {
				Setters = {
					new Setter(TextBlock.FontSizeProperty, pts(10d)),
					new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
				}
			}},
		};

		private Waybill waybill;
		private Settings settings;
		private WaybillSettings waybillSettings;
		private IList<WaybillLine> lines;

		public PriceTagDocument(Waybill waybill, IList<WaybillLine> lines, Settings settings)
		{
			this.waybill = waybill;
			this.waybillSettings = waybill.WaybillSettings;
			this.settings = settings;
			this.lines = lines;
		}

		protected override void Configure()
		{
			//отступы должны быть тк большинство принтеров требует их
			doc.PagePadding = new Thickness(0, 0, 0, 0);
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			//мы должны оставить место для "шапки" и "подвала"
			paginator.PageSize = new Size(paginator.PageSize.Width,
				paginator.PageSize.Height);
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
				Height = pts(106),
				Width = pts(162),
			};
			var product = new TextBlock {
				Text = line.Product,
				FontSize = pts(11),
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				Height = pts(37),
				Width = pts(160)
			};
			product.SetValue(Canvas.LeftProperty, 0d);
			product.SetValue(Canvas.TopProperty, 0d);
			canvas.Children.Add(product);

			var costLabel = new TextBlock {
				Text = "Цена",
				Width = pts(26),
				FontSize = pts(9)
			};
			costLabel.SetValue(Canvas.LeftProperty, pts(0d));
			costLabel.SetValue(Canvas.TopProperty, pts(38d));
			canvas.Children.Add(costLabel);

			var cost = new TextBlock {
				Text = FormatCost(line),
				Width = pts(133),
				FontSize = pts(11),
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextAlignment = TextAlignment.Right,
			};
			cost.SetValue(Canvas.LeftProperty, pts(28d));
			cost.SetValue(Canvas.TopProperty, pts(38d));
			canvas.Children.Add(cost);

			var producerLabel = new TextBlock {
				Text = "Произв.",
				Width = pts(37),
				FontSize = pts(9)
			};
			producerLabel.SetValue(Canvas.LeftProperty, 0d);
			producerLabel.SetValue(Canvas.TopProperty, pts(50d));
			canvas.Children.Add(producerLabel);
			var country = new TextBlock {
				Text = line.Country,
				Width = pts(122),
				FontSize = pts(9),
				TextAlignment = TextAlignment.Right,
			};
			country.SetValue(Canvas.LeftProperty, pts(39d));
			country.SetValue(Canvas.TopProperty, pts(50d));
			canvas.Children.Add(country);

			var producer = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = pts(9),
				Text = line.Producer,
				Width = pts(160),
			};
			producer.SetValue(Canvas.LeftProperty, 0d);
			producer.SetValue(Canvas.TopProperty, pts(62d));
			canvas.Children.Add(producer);

			var periodLabel = new TextBlock {
				Text = "Срок годности",
				Width = pts(65),
				FontSize = pts(9)
			};
			periodLabel.SetValue(Canvas.LeftProperty, 0d);
			periodLabel.SetValue(Canvas.TopProperty, pts(73d));
			canvas.Children.Add(periodLabel);
			var period = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = pts(9),
				Text = line.Period,
				Width = pts(93),
			};
			period.SetValue(Canvas.LeftProperty, pts(67d));
			period.SetValue(Canvas.TopProperty, pts(73d));
			canvas.Children.Add(period);

			var singLabel = new TextBlock {
				HorizontalAlignment = HorizontalAlignment.Right,
				Text = "Подпись",
				Width = pts(160),
				FontSize = pts(9)
			};
			singLabel.SetValue(Canvas.LeftProperty, 0d);
			singLabel.SetValue(Canvas.TopProperty, pts(84d));
			canvas.Children.Add(singLabel);

			var waybillDate = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = pts(9),
				Text = line.Waybill.DocumentDate.ToShortDateString(),
				Width = pts(100),
			};
			waybillDate.SetValue(Canvas.LeftProperty, pts(60d));
			waybillDate.SetValue(Canvas.TopProperty, pts(84d));
			canvas.Children.Add(waybillDate);

			ApplyDefaults(canvas);

			return canvas;
		}

		public FrameworkElement Big(WaybillLine line)
		{
			var canvas = new Canvas {
				Width = pts(162),
				Height = pts(106),
			};
			var nameAndAddressLabel = new TextBlock {
				Text = waybillSettings.FullName,
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				FontSize = pts(8),
				Width = pts(162),
				Height = pts(20)
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
				FontSize = pts(11),
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				Height = pts(37),
				Width = pts(160)
			};
			product.SetValue(Canvas.LeftProperty, 0d);
			product.SetValue(Canvas.TopProperty, pts(21d));
			canvas.Children.Add(product);

			var cost = new TextBlock {
				Text = FormatCost(line),
				Width = pts(112),
				FontSize = pts(26),
				FontWeight = FontWeights.Bold,
				TextAlignment = TextAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			cost.SetValue(Canvas.LeftProperty, pts(49d));
			cost.SetValue(Canvas.TopProperty, pts(78d));
			canvas.Children.Add(cost);

			canvas.Add(0, pts(68), new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = pts(8),
				Text = "Годен до",
				Width = pts(49),
			});

			canvas.Add(0, pts(76), new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = pts(9),
				Text = line.Period,
				Width = pts(49),
			});

			canvas.Add(0, pts(86), new TextBlock {
				FontSize = pts(8),
				Text = "Подпись",
				Width = pts(49),
			});

			canvas.Add(0, pts(94), new TextBlock {
				FontSize = pts(9),
				Text = DateTime.Now.ToShortDateString(),
				Width = pts(49),
			});

			ApplyDefaults(canvas);

			return canvas;
		}

		public FrameworkElement Big2(WaybillLine line)
		{
			var canvas = new Canvas {
				Width = pts(162),
				Height = pts(106),
			};

			var nameAndAddressLabel = new TextBlock {
				Text = waybillSettings.FullName,
				TextAlignment = TextAlignment.Center,
				FontSize = pts(6),
				Width = pts(162),
				Height = pts(10)
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
				FontSize = pts(6),
				Width = pts(81),
				Height = pts(10)
			};
			var serialNumberBorder = new Border {
				BorderThickness = new Thickness(0, 0, 1, 1),
				BorderBrush = Brushes.Black,
				Child = serialNumberLabel
			};
			serialNumberBorder.SetValue(Canvas.LeftProperty, 0d);
			serialNumberBorder.SetValue(Canvas.TopProperty, pts(10d));
			canvas.Children.Add(serialNumberBorder);

			var supplierNameLabel = new TextBlock {
				Text = line.Waybill.SupplierName,
				TextAlignment = TextAlignment.Center,
				FontSize = pts(6),
				Width = pts(81),
				Height = pts(10)
			};
			var supplierNameBorder = new Border {
				BorderThickness = new Thickness(0, 0, 0, 1),
				BorderBrush = Brushes.Black,
				Child = supplierNameLabel
			};
			supplierNameBorder.SetValue(Canvas.LeftProperty, pts(81d));
			supplierNameBorder.SetValue(Canvas.TopProperty, pts(10d));
			canvas.Children.Add(supplierNameBorder);

			var product = new TextBlock {
				Text = $"{line.Product}\n{line.Producer}",
				FontSize = pts(11),
				FontWeight = FontWeights.Bold,
				TextDecorations = TextDecorations.Underline,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				Height = pts(54),
				Width = pts(160)
			};
			product.SetValue(Canvas.LeftProperty, 0d);
			product.SetValue(Canvas.TopProperty, pts(20d));
			canvas.Children.Add(product);

			var cost = new TextBlock {
				Text = FormatCost(line),
				Width = pts(112),
				FontSize = pts(26),
				FontWeight = FontWeights.Bold,
				TextAlignment = TextAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			cost.SetValue(Canvas.LeftProperty, pts(49d));
			cost.SetValue(Canvas.TopProperty, pts(78d));
			canvas.Children.Add(cost);

			var periodLabel = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = pts(8),
				Text = "Годен до",
				Width = pts(49),
			};
			periodLabel.SetValue(Canvas.LeftProperty, 0d);
			periodLabel.SetValue(Canvas.TopProperty, pts(78d));
			canvas.Children.Add(periodLabel);
			var period = new TextBlock {
				TextAlignment = TextAlignment.Right,
				FontSize = pts(9),
				Text = line.Period,
				Width = pts(49),
			};
			period.SetValue(Canvas.LeftProperty, 0d);
			period.SetValue(Canvas.TopProperty, pts(86d));
			canvas.Children.Add(period);

			var singLabel = new TextBlock {
				FontSize = pts(8),
				Text = "Подпись",
				Width = pts(49),
			};
			singLabel.SetValue(Canvas.LeftProperty, 0d);
			singLabel.SetValue(Canvas.TopProperty, pts(96d));
			canvas.Children.Add(singLabel);

			ApplyDefaults(canvas);
			return canvas;
		}

		private FrameworkElement Normal(WaybillLine line)
		{
			var panel = new StackPanel {
				Width = pts(162),
				Margin = new Thickness(2)
			};
			if (!settings.PriceTag.HideNotPrinted || settings.PriceTag.PrintFullName) {
				var uri = new Uri(String.Format(@"pack://application:,,,/{0};component/assets/images/price-tag-log.png",
					typeof(PriceTagDocument).Assembly.GetName().Name));
				panel.Children.Add(new Border {
					BorderThickness = new Thickness(0, 0, 0, 0.5),
					BorderBrush = Brushes.Black,
					Child = new DockPanel {
						Width = pts(162),
						Children = {
							new Image {
								Height = pts(20),
								Width = pts(18),
								Source = new BitmapImage(uri)
							},
							new TextBlock {
								Height = pts(20),
								Text = settings.PriceTag.PrintFullName ? waybillSettings.FullName : "",
								FontSize = pts(8),
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
				var value = String.Format("{0:d} {1}",
					settings.PriceTag.PrintDocumentDate ? (DateTime?)line.Waybill.DocumentDate : null,
					settings.PriceTag.PrintSupplier ? line.Waybill.SupplierName : "");

				var label = new Label {
					FontSize = pts(8),
					Content = value,
					HorizontalContentAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(0),
					Padding = new Thickness(0)
				};
				var dockPanel = new DockPanel {
					Width = pts(162),
					Children = {
						new Label {
							VerticalAlignment = VerticalAlignment.Center,
							Content = "Подпись",
							FontSize = pts(8),
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
					Width = pts(162),
					Children = {
						new Label {
							VerticalAlignment = VerticalAlignment.Center,
							Content = name,
							FontSize = pts(9),
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