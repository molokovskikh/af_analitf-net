using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Binders
{
	public class VisibilityBinder
	{
		private static ElementConvention convention;

		static VisibilityBinder()
		{
			convention = new ElementConvention {
				ElementType = typeof(FrameworkElement),
				GetBindableProperty = element => UIElement.VisibilityProperty,
				ParameterProperty = "Visibility",
			};
		}

		public static void Bind(IEnumerable<FrameworkElement> elements, Type type)
		{
			foreach (var element in elements) {
				var propertyName = element.Name + "Visible";
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