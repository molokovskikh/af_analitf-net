using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using EventTrigger = System.Windows.Interactivity.EventTrigger;

namespace AnalitF.Net.Client.Binders
{
	public class EnabledBinder
	{
		private static ElementConvention convention;

		static EnabledBinder()
		{
			convention = new ElementConvention {
				ElementType = typeof(FrameworkElement),
				GetBindableProperty = element => UIElement.IsEnabledProperty,
				ParameterProperty = "IsEnabled",
				CreateTrigger = () => new EventTrigger { EventName = "IsEnabledChanged" }
			};
		}

		public static void Bind(IEnumerable<FrameworkElement> elements, Type type)
		{
			foreach (var element in elements) {
				var propertyName = element.Name + "Enabled";
				var property = type
					.GetPropertyCaseInsensitive(propertyName);
				if (property != null) {
					ConventionManager.SetBindingWithoutBindingOverwrite(
						type,
						propertyName,
						property,
						element,
						convention,
						convention.GetBindableProperty(element));
				}
			}
		}
	}
}