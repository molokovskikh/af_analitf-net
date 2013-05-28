using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class SimpleSettings : Screen
	{
		private object settings;

		public SimpleSettings(object settings)
		{
			this.settings = settings;
			DisplayName = "АналитФАРМАЦИЯ";
			var attributes = settings.GetType().GetCustomAttributes(typeof(DescriptionAttribute), true);
			if (attributes.Length > 0) {
				DisplayName = ((DescriptionAttribute)attributes[0]).Description;
			}
		}

		public void OK()
		{
			TryClose(true);
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var properties = settings.GetType()
				.GetProperties()
				.Select(p => Tuple.Create(p, p.GetCustomAttributes(typeof(DisplayAttribute), true)))
				.Where(t => t.Item2.Length > 0)
				.OrderBy(t => ((DisplayAttribute)t.Item2[0]).Order)
				.ToArray();

			var grid = (Grid)((FrameworkElement)view).FindName("Data");
			grid.DataContext = settings;
			for(var i = 0; i < properties.Length; i ++) {
				grid.RowDefinitions.Add(new RowDefinition());
				var property = properties[i];
				var label = new Label {
					Content = ((DisplayAttribute)property.Item2[0]).Name
				};
				label.SetValue(Grid.RowProperty, i);
				label.SetValue(Grid.ColumnProperty, 0);
				grid.Children.Add(label);

				UIElement input;
				if (property.Item1.PropertyType == typeof(DateTime)) {
					input = new DatePicker();
					BindingOperations.SetBinding(input, DatePicker.SelectedDateProperty, new Binding(property.Item1.Name));
				}
				else {
					input = new TextBox();
					BindingOperations.SetBinding(input, TextBox.TextProperty, new Binding(property.Item1.Name));
				}
				input.SetValue(Grid.RowProperty, i);
				input.SetValue(Grid.ColumnProperty, 1);
				grid.Children.Add(input);
			}
		}
	}
}