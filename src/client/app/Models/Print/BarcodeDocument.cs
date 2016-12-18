using System;
using System.Collections.Generic;
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
					new RowDefinition(),
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
				}
			};

			if (line.AltBarcode?.Length == 12) {
				var img = new Barcode().Encode(TYPE.UPCA, line.AltBarcode, 100, 18);
				ImageSource imageSource;
				using (var stream = new MemoryStream()) {
					img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
					stream.Position = 0;
					imageSource = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
				}
				var barcode = new Image {
					Source = imageSource,
				};
				barcode.SetValue(Grid.ColumnSpanProperty, 2);
				panel.Children.Add(barcode);
			}

			var label1 = new TextBlock
			{
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				FontSize = 9,
				Text = line.Product,
			};
			label1.SetValue(Grid.RowProperty, 1);
			label1.SetValue(Grid.ColumnSpanProperty, 2);
			panel.Children.Add(label1);

			var label2 = new TextBlock
			{
				FontSize = 7,
				Text = line.AltBarcode,
			};
			label2.SetValue(Grid.RowProperty, 2);
			panel.Children.Add(label2);

			var label3 = new Label
			{
				FontSize = 14,
				FontWeight = FontWeights.Bold,
				Content = line.RetailCost?.ToString("0.00"),
				HorizontalAlignment = HorizontalAlignment.Right,
			};
			label3.SetValue(Grid.RowProperty, 2);
			label3.SetValue(Grid.ColumnProperty, 1);
			label3.SetValue(Grid.RowSpanProperty, 2);
			panel.Children.Add(label3);

			var label4 = new TextBlock
			{
				FontSize = 7,
				Text = line.SerialNumber,
			};
			label4.SetValue(Grid.RowProperty, 3);
			panel.Children.Add(label4);

			var label5 = new TextBlock
			{
				Text = line.ProviderDocumentId,
				FontSize = 7
			};
			label5.SetValue(Grid.RowProperty, 4);
			panel.Children.Add(label5);

			var label6 = new TextBlock
			{
				FontSize = 7,
				Text = line.DocumentDate.ToShortDateString(),
				HorizontalAlignment = HorizontalAlignment.Right,
			};
			label6.SetValue(Grid.RowProperty, 4);
			label6.SetValue(Grid.ColumnProperty, 1);
			panel.Children.Add(label6);

			return panel;
		}
	}
}