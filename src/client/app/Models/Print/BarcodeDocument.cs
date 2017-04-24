using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BarcodeLib;
using System.IO;

namespace AnalitF.Net.Client.Models.Print
{
	public class BarcodeDocument
	{
		private Settings settings;
		private List<TagPrintable> lines = new List<TagPrintable>();

		public BarcodeDocument(IList<TagPrintable> lines, Settings settings)
		{
			this.settings = settings;
			foreach (var line in lines) {
				for (int i = 0; i < line.CopyCount; i++) {
					this.lines.Add(line);
				}
			}
		}

		public FixedDocument Build()
		{
			Func<TagPrintable, FrameworkElement> map = Normal;
			var borderThickness = 0.5d;
			return FixedDocumentHelper.BuildFixedDoc(lines, l => Border(map(l), borderThickness), borderThickness);
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

		private BitmapImage MakeBarcode(string barcode, int width, int height)
		{
			BitmapImage result = null;
			if (barcode?.Length != 13)
				return result;
			// не любая строка из 13 цифр является валидным штрихкодом, как валидировать - неизвестно
			try
			{
				using (var barcodeImage = Barcode.DoEncode(TYPE.EAN13, barcode, false, width, height)) {
					using (var stream = new MemoryStream()) {
						barcodeImage.Save(stream, ImageFormat.Bmp);
						stream.Position = 0;
						result = new BitmapImage();
						result.BeginInit();
						result.CacheOption = BitmapCacheOption.OnLoad;
						result.StreamSource = stream;
						result.EndInit();
					}
				}
			}
			catch
			{
			}
			return result;
		}

		private FrameworkElement Normal(TagPrintable line)
		{
			var panel = new Grid
			{
				Width = 100,
				Height = 66,
				Margin = new Thickness(1),
				ColumnDefinitions = {
					new ColumnDefinition(),
					new ColumnDefinition()
				},
				RowDefinitions = {
					new RowDefinition { Height = new GridLength(16, GridUnitType.Pixel) },
					new RowDefinition { Height = new GridLength(26, GridUnitType.Pixel) },
					new RowDefinition { Height = new GridLength(8, GridUnitType.Pixel) },
					new RowDefinition { Height = new GridLength(8, GridUnitType.Pixel) },
					new RowDefinition { Height = new GridLength(8, GridUnitType.Pixel) },
				}
			};

			var barcode = MakeBarcode(line.Barcode, 100, 16);
			if (barcode != null) {
				var img = new Image() {
					Source = barcode,
				};
				img.SetValue(Grid.ColumnSpanProperty, 2);
				panel.Children.Add(img);
			}

			var label1 = new TextBlock
			{
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				FontSize = 8,
				Text = line.Product,
				Margin = new Thickness(0, 2, 0, 0),
				Padding = new Thickness(0),
				LineHeight = 8,
				LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
				Height = 24,
			};
			label1.SetValue(Grid.RowProperty, 1);
			label1.SetValue(Grid.ColumnSpanProperty, 2);
			panel.Children.Add(label1);

			var label2 = new TextBlock
			{
				FontSize = 7,
				Text = line.Barcode,
				Margin = new Thickness(0),
				Padding = new Thickness(0),
			};
			label2.SetValue(Grid.RowProperty, 2);
			panel.Children.Add(label2);

			var label3 = new Label
			{
				FontSize = 12,
				FontWeight = FontWeights.Bold,
				Content = line.RetailCost?.ToString("0.00"),
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0),
				Padding = new Thickness(0),
			};
			label3.SetValue(Grid.RowProperty, 2);
			label3.SetValue(Grid.ColumnProperty, 1);
			label3.SetValue(Grid.RowSpanProperty, 2);
			panel.Children.Add(label3);

			var label4 = new TextBlock
			{
				FontSize = 7,
				Text = line.SerialNumber,
				Margin = new Thickness(0),
				Padding = new Thickness(0),
			};
			label4.SetValue(Grid.RowProperty, 3);
			panel.Children.Add(label4);

			var label5 = new TextBlock
			{
				Text = line.ProviderDocumentId,
				FontSize = 7,
				Margin = new Thickness(0),
				Padding = new Thickness(0),
			};
			label5.SetValue(Grid.RowProperty, 4);
			panel.Children.Add(label5);

			var label6 = new TextBlock
			{
				FontSize = 7,
				Text = line.DocumentDate.ToShortDateString(),
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0),
				Padding = new Thickness(0),
			};
			label6.SetValue(Grid.RowProperty, 4);
			label6.SetValue(Grid.ColumnProperty, 1);
			panel.Children.Add(label6);

			return panel;
		}
	}
}