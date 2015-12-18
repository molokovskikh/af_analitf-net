using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class OrderLinesView : UserControl
	{
		public Dictionary<DependencyObject, DependencyProperty> persistable =
			new Dictionary<DependencyObject, DependencyProperty>();

		private GridLength height = new GridLength(1, GridUnitType.Star);

		public OrderLinesView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
				Persist(Expander, Expander.IsExpandedProperty);
				Persist(OrdersGrid.RowDefinitions[Grid.GetRow(Expander)], RowDefinition.HeightProperty);
				Persist(OrdersGrid.RowDefinitions[Grid.GetRow(Lines)], RowDefinition.HeightProperty);
				Restore();
			};

			Unloaded += (sender, args) => {
				Save();
			};

			DataGridHelper.CalculateColumnWidths(Lines);
			DataGridHelper.CalculateColumnWidths(SentLines);
			new Editable().Attach(Lines);

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			ExpandedCollapsed(Expander, null);
		}

		private void Restore()
		{
			var model = DataContext as BaseScreen;
			if (model == null)
				return;
			foreach (var property in persistable) {
				var prop = property.Value;
				var key = GetKey(property);
				if (!model.Shell.PersistentContext.ContainsKey(key))
					return;
				var value = model.Shell.PersistentContext[key];
				if (prop.IsValidType(value))
					property.Key.SetValue(prop, value);
				if (value is JObject) {
					property.Key.SetValue(prop, ((JObject)value).ToObject(prop.PropertyType));
				}
			}
		}

		private void Save()
		{
			var model = DataContext as BaseScreen;
			if (model == null)
				return;
			foreach (var property in persistable) {
				var prop = property.Value;
				model.Shell.PersistentContext[GetKey(property)] = property.Key.GetValue(prop);
			}
		}

		private static string GetKey(KeyValuePair<DependencyObject, DependencyProperty> prop)
		{
			var name = (prop.Key as FrameworkElement)?.Name
				?? (prop.Key as FrameworkContentElement)?.Name;
			if (String.IsNullOrEmpty(name))
				return "OrderLinesView." + prop.Value.Name;
			return $"OrderLinesView.{name}.{prop.Value.Name}";
		}

		private void Persist(DependencyObject o, DependencyProperty property)
		{
			persistable.Add(o, property);
		}

		public void ApplyStyles()
		{
			var context = "";
			var baseScreen = ((BaseScreen)DataContext);
			if (baseScreen.User != null && baseScreen.User.IsPreprocessOrders)
				context = "CorrectionEnabled";
			StyleHelper.ApplyStyles(typeof(OrderLine), Lines, Application.Current.Resources, Legend, context);

			if (baseScreen.Settings.Value != null && baseScreen.Settings.Value.HighlightUnmatchedOrderLines)
				StyleHelper.ApplyStyles(typeof(SentOrderLine), SentLines, Application.Current.Resources, Legend);

			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
		}

		private void ExpandedCollapsed(object sender, RoutedEventArgs e)
		{
			var expander = (Expander)sender;
			var row = OrdersGrid.RowDefinitions[Grid.GetRow(expander)];
			if (expander.IsExpanded) {
				row.Height = height;
			}
			else {
				height = row.Height;
				row.Height = GridLength.Auto;
			}
		}
	}
}
