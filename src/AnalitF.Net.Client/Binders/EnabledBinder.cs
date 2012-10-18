using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public class EnabledBinder
	{
		public static void Bind(IEnumerable<FrameworkElement> elements, Type type)
		{
			foreach (var frameworkElement in elements) {
				var propertyName = frameworkElement.Name + "Enabled";
				var property = type
					.GetPropertyCaseInsensitive(propertyName);
				if (property != null) {
					var convention = ConventionManager.GetElementConvention(typeof(FrameworkElement));
					ConventionManager.SetBindingWithoutBindingOverwrite(
						type,
						propertyName,
						property,
						frameworkElement,
						convention,
						convention.GetBindableProperty(frameworkElement));
				}
			}
		}
	}
}