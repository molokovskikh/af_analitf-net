using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class SimpleSettings : Screen, ICancelable
	{
		public object Target;
		public Tuple<PropertyInfo, object[]>[] Properties;

		public SimpleSettings(object settings)
		{
			this.Target = settings;
			DisplayName = "АналитФАРМАЦИЯ";
			var attributes = settings.GetType().GetCustomAttributes(typeof(DescriptionAttribute), true);
			if (attributes.Length > 0) {
				DisplayName = ((DescriptionAttribute)attributes[0]).Description;
			}
			Properties = settings.GetType()
				.GetProperties()
				.Select(p => Tuple.Create(p, p.GetCustomAttributes(typeof(DisplayAttribute), true)))
				.Where(t => t.Item2.Length > 0)
				.OrderBy(t => ((DisplayAttribute)t.Item2[0]).Order)
				.ToArray();
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }

		public void Ok()
		{
			TryClose(false);
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var grid = (Grid)((FrameworkElement)view).FindName("Data");
			grid.DataContext = Target;
			for(var i = 0; i < Properties.Length; i ++) {
				grid.RowDefinitions.Add(new RowDefinition());
				var property = Properties[i];
				var label = new Label {
					Content = ((DisplayAttribute)property.Item2[0]).Name
				};
				label.SetValue(Grid.RowProperty, i);
				label.SetValue(Grid.ColumnProperty, 0);
				grid.Children.Add(label);

				UIElement input;
				DependencyProperty inputProperty;
				if (property.Item1.PropertyType == typeof(DateTime)) {
					input = new DatePicker();
					inputProperty = DatePicker.SelectedDateProperty;
				}
				else {
					input = new TextBox();
					inputProperty = TextBox.TextProperty;
				}
				BindingOperations.SetBinding(input, inputProperty, new Binding(property.Item1.Name));
				input.SetValue(Grid.RowProperty, i);
				input.SetValue(Grid.ColumnProperty, 1);
				grid.Children.Add(input);
			}
		}
	}
}