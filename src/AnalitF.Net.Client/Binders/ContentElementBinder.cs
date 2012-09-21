using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using AnalitF.Net.Client.Extentions;
using Caliburn.Micro;
using Common.Tools;

namespace AnalitF.Net.Client.Binders
{
	public class ContentElementBinder
	{

		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			ConventionManager.AddElementConvention<Run>(Run.TextProperty, "Text", "DataContextChanged");
			var viewModelType = viewModel.GetType();
			var elements = XamlExtentions
				.DeepChildren(view)
				.OfType<FrameworkContentElement>()
				.Where(e => !string.IsNullOrEmpty(e.Name))
				.Distinct()
				.ToList();

			foreach (var element in elements) {
				var cleanName = element.Name.Trim('_');
				var parts = cleanName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

				var property = viewModelType.GetPropertyCaseInsensitive(parts[0]);
				var interpretedViewModelType = viewModelType;

				for(int i = 1; i < parts.Length && property != null; i++) {
					interpretedViewModelType = property.PropertyType;
					property = interpretedViewModelType.GetPropertyCaseInsensitive(parts[i]);
				}

				if(property == null) {
					//Log.Info("Binding Convention Not Applied: Element {0} did not match a property.", element.Name);
					continue;
				}

				var convention = ConventionManager.GetElementConvention(element.GetType());
				if(convention == null) {
					//Log.Warn("Binding Convention Not Applied: No conventions configured for {0}.", element.GetType());
					continue;
				}
				Bind(convention, element, cleanName, property, viewModelType);
			}
		}

		private static bool Bind(ElementConvention convention, FrameworkContentElement element, string cleanName, PropertyInfo property, Type viewModelType)
		{
			var bindableProperty = convention.GetBindableProperty(element);

			if (bindableProperty == null || element.GetBindingExpression(bindableProperty) != null) {
				return false;
			}

			var path = cleanName.Replace('_', '.');

			var binding = new Binding(path);

			ConventionManager.ApplyBindingMode(binding, property);
			ConventionManager.ApplyValueConverter(binding, bindableProperty, property);
			ConventionManager.ApplyStringFormat(binding, convention, property);
			ConventionManager.ApplyValidation(binding, viewModelType, property);
			ConventionManager.ApplyUpdateSourceTrigger(bindableProperty, element, binding, property);

			BindingOperations.SetBinding(element, bindableProperty, binding);
			return true;
		}
	}
}