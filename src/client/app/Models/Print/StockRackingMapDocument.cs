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
using AnalitF.Net.Client.ViewModels.Inventory;
using NHibernate;

namespace AnalitF.Net.Client.Models.Print
{
	class StockRackingMapDocument
	{
		private IList<Stock> _stocks;
		private ISession _session;

		public StockRackingMapDocument(ISession session, IList<Stock> stocks)
		{
			_session = session;
			_stocks = stocks;
		}

		public FixedDocument Build()
		{
			Func<Stock, FrameworkElement> map = Normal;

			return FixedDocumentHelper.BuildFixedLandscapeDoc(_stocks, l => Border(map(l), 0.5), 0.5);
		}

		private FrameworkElement Normal(Stock line)
		{
			var panel = new Grid
			{
				Width = 300,
				Height = 200,
				Margin = new Thickness(10)
			};

			RowDefinition r1 = new RowDefinition();
			r1.Height = GridLength.Auto;
			RowDefinition r2 = new RowDefinition();
			r2.Height = new GridLength(1, GridUnitType.Star);
			RowDefinition r3 = new RowDefinition();
			r3.Height = GridLength.Auto;
			RowDefinition r4 = new RowDefinition();
			r4.Height = GridLength.Auto;
			panel.RowDefinitions.Add(r1);
			panel.RowDefinitions.Add(r2);
			panel.RowDefinitions.Add(r3);
			panel.RowDefinitions.Add(r4);

			var label1 = new TextBlock
			{
				FontSize = 10,
				Text = "Поставка №" + line.ReceivingOrderId,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(2, 2, 0, 0),
				Padding = new Thickness(0)
			};

			var label2 = new TextBlock
			{
				FontSize = 10,
				Text = "Накладная №" + line.WaybillNumber,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(2, 17, 0, 0),
				Padding = new Thickness(0)
			};

			var label3 = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				FontSize = 10,
				Text = "Поставщик " + GetReceivingOrder(_session, line.ReceivingOrderId).Supplier.FullName,
				Margin = new Thickness(2, 32, 0, 0),
				Padding = new Thickness(0)
			};

			var label4 = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				FontSize = 10,
				Text = line.DocumentDate,
				Margin = new Thickness(0, 0, 25, 0),
				Padding = new Thickness(0)
			};

			var label5 = new TextBlock
			{
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				FontWeight = FontWeights.Bold,
				FontSize = 11,
				Text = line.Product,
				Margin = new Thickness(0),
				Padding = new Thickness(0),
				VerticalAlignment = VerticalAlignment.Center
			};
			label5.SetValue(Grid.RowProperty, 1);

			var label6 = new TextBlock
			{
				FontSize = 10,
				Text = "Штрих-код " + line.Barcode,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(2, 2, 0, 0),
				Padding = new Thickness(0)
			};
			label6.SetValue(Grid.RowProperty, 2);

			var label7 = new TextBlock
			{
				FontSize = 10,
				Text = "Серия " + line.Seria,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(2, 17, 0, 0),
				Padding = new Thickness(0)
			};
			label7.SetValue(Grid.RowProperty, 2);

			var label8 = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				FontSize = 10,
				Text = "Количество " + line.Count.ToString("N2"),
				Margin = new Thickness(0, 2, 25, 0),
				Padding = new Thickness(0)
			};
			label8.SetValue(Grid.RowProperty, 2);

			var label9 = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				FontSize = 10,
				Text = "Срок годности " + line.Period,
				Margin = new Thickness(0, 17, 25, 0),
				Padding = new Thickness(0)
			};
			label9.SetValue(Grid.RowProperty, 2);

			var label10 = new Label
			{
				Width = 60,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				FontSize = 10,
				Content = "Поставщик",
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(2, 32, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label10.SetValue(Grid.RowProperty, 3);

			var label11 = new Label
			{
				Width = 60,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				FontSize = 10,
				Content = "Аптека",
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(2, 52, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label11.SetValue(Grid.RowProperty, 3);

			var label12 = new Label
			{
				Width = 40,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				FontSize = 10,
				Content = "Нац. %",
				Margin = new Thickness(61.5, 12.25, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label12.SetValue(Grid.RowProperty, 3);

			var label13 = new Label
			{
				Width = 60,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				FontSize = 10,
				Content = "Цена",
				Margin = new Thickness(101, 12.25, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label13.SetValue(Grid.RowProperty, 3);

			var label14 = new Label
			{
				Width = 40,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				FontSize = 10,
				Content = "0",
				Margin = new Thickness(61.5, 32, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label14.SetValue(Grid.RowProperty, 3);

			var label15 = new Label
			{
				Width = 40,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				FontSize = 10,
				Content = line.RetailMarkup,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(61.5, 52, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label15.SetValue(Grid.RowProperty, 3);

			var label16 = new Label
			{
				Width = 60,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				FontSize = 10,
				Content = line.Cost,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(101, 32, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label16.SetValue(Grid.RowProperty, 3);

			var label17 = new Label
			{
				Width = 60,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left,
				FontSize = 10,
				Content = line.RetailCost,
				Margin = new Thickness(101, 52, 0, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label17.SetValue(Grid.RowProperty, 3);

			var label18 = new Label
			{
				Width = 60,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				FontSize = 10,
				Content = "НДС " + line.NdsPers.ToString("P"),
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0, 22, 25, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label18.SetValue(Grid.RowProperty, 3);

			var label19 = new Label
			{
				Width = 60,
				BorderThickness = new Thickness(0.5),
				BorderBrush = Brushes.Black,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				FontSize = 10,
				Content = "НП " + line.NpPers.ToString("P"),
				Margin = new Thickness(0, 47, 25, 0),
				Padding = new Thickness(3),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center
			};
			label19.SetValue(Grid.RowProperty, 3);

			panel.Children.Add(label1);
			panel.Children.Add(label2);
			panel.Children.Add(label3);
			panel.Children.Add(label4);
			panel.Children.Add(label5);
			panel.Children.Add(label6);
			panel.Children.Add(label7);
			panel.Children.Add(label8);
			panel.Children.Add(label9);
			panel.Children.Add(label10);
			panel.Children.Add(label11);
			panel.Children.Add(label12);
			panel.Children.Add(label13);
			panel.Children.Add(label14);
			panel.Children.Add(label15);
			panel.Children.Add(label16);
			panel.Children.Add(label17);
			panel.Children.Add(label18);
			panel.Children.Add(label19);

			return panel;
		}

		private static Border Border(FrameworkElement element, double borderThickness)
		{
			var border = new Border
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, borderThickness, borderThickness),
				Child = element
			};
			return border;
		}

		public ReceivingOrder GetReceivingOrder(ISession session, uint? id)
		{
			return session.Get<ReceivingOrder>(id);
		}
	}
}
