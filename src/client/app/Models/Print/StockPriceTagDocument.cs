using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Models.Print
{
	class StockPriceTagDocument
	{
		private IList<Stock> _stocks;
		private string _name;

		public StockPriceTagDocument(IList<Stock> stocks, string name)
		{
			_stocks = stocks;
			_name = name;
		}

		public FixedDocument Build()
		{
			Func<Stock, FrameworkElement> map = Normal;

			return FixedDocumentHelper.BuildFixedDoc(_stocks, l => Border(map(l), 0.5), 0.5);
		}

		private FrameworkElement Normal(Stock line)
		{
			var panel = new Grid
			{
				Width = 140,
				Height = 100,
				Margin = new Thickness(2)
			};

			RowDefinition r1 = new RowDefinition();
			r1.Height = GridLength.Auto;
			RowDefinition r2 = new RowDefinition();
			r2.Height = new GridLength(1, GridUnitType.Star);
			RowDefinition r3 = new RowDefinition();
			r3.Height = GridLength.Auto;
			panel.RowDefinitions.Add(r1);
			panel.RowDefinitions.Add(r2);
			panel.RowDefinitions.Add(r3);

			var label1 = new TextBlock
			{
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				FontSize = 10,
				Text = _name,
				Margin = new Thickness(0),
				Padding = new Thickness(0)
			};
			label1.SetValue(Grid.RowProperty, 0);

			var label2 = new TextBlock
			{
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				FontWeight = FontWeights.Bold,
				FontSize = 11,
				Text = line.Product + "\n" + line.RetailCost,
				Margin = new Thickness(0),
				Padding = new Thickness(0),
				VerticalAlignment = VerticalAlignment.Center
			};
			label2.SetValue(Grid.RowProperty, 1);

			var label3 = new TextBlock
			{
				TextAlignment = TextAlignment.Left,
				TextWrapping = TextWrapping.Wrap,
				FontSize = 10,
				Text = "Партия №",
				Margin = new Thickness(0),
				Padding = new Thickness(0)
			};
			label3.SetValue(Grid.RowProperty, 2);

			panel.Children.Add(label1);
			panel.Children.Add(label2);
			panel.Children.Add(label3);

			return panel;
		}

		private static Border Border(FrameworkElement element, double borderThickness)
		{
			var border = new Border
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, borderThickness, borderThickness),
				Child = element,
			};
			return border;
		}
	}
}
