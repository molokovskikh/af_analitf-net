using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Common.Tools;
using ReactiveUI;
using Xceed.Wpf.Toolkit;

namespace AnalitF.Net.Client.Binders
{
	public class ContentElementBinder
	{
		public static readonly DependencyProperty PasswordProperty =
			DependencyProperty.RegisterAttached("Password",
				typeof(string),
				typeof(ContentElementBinder),
				new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnPasswordPropertyChanged));

		private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var passwordBox = sender as PasswordBox;
			passwordBox.PasswordChanged -= PasswordChanged;

			if (!Equals(passwordBox.Password, e.NewValue))
				passwordBox.Password = (string)e.NewValue;

			passwordBox.PasswordChanged += PasswordChanged;
		}

		public static string GetPassword(DependencyObject dp)
		{
			return (string)dp.GetValue(PasswordProperty);
		}

		public static void SetPassword(DependencyObject dp, string value)
		{
			dp.SetValue(PasswordProperty, value);
		}

		private static void PasswordChanged(object sender, RoutedEventArgs e)
		{
			var passwordBox = sender as PasswordBox;
			SetPassword(passwordBox, passwordBox.Password);
		}

		public static void Register()
		{
			ConventionManager.Singularize = s => {
				if (s == "Taxes")
					return "Tax";
				if (s == "Value")
					return s;
				return s.InflectTo().Singularized;
			};
			ConventionManager.AddElementConvention<SplitButton>(SplitButton.ContentProperty, "DataContext", "Click");
			ConventionManager.AddElementConvention<Run>(Run.TextProperty, "Text", "DataContextChanged");
			ConventionManager.AddElementConvention<IntegerUpDown>(IntegerUpDown.ValueProperty, "Value", "ValueChanged");
			ConventionManager.AddElementConvention<FlowDocumentScrollViewer>(FlowDocumentScrollViewer.DocumentProperty, "Document ", "DataContextChanged");
			ConventionManager.AddElementConvention<DocumentViewerBase>(DocumentViewerBase.DocumentProperty, "Document ", "DataContextChanged");
			ConventionManager.AddElementConvention<PasswordBox>(PasswordProperty, "Password", "PasswordChanged");
			ConventionManager.AddElementConvention<MultiSelector>(Selector.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) =>
				{
					var parentApplied = ConventionManager.GetElementConvention(typeof(Selector))
						.ApplyBinding(viewModelType, path, property, element, convention);
					var index = path.LastIndexOf('.');
					index = index == -1 ? 0 : index + 1;
					var baseName = path.Substring(index);
					var propertyInfo = viewModelType.GetPropertyCaseInsensitive("Selected" + baseName);

					if (propertyInfo == null || !typeof(IList).IsAssignableFrom(propertyInfo.PropertyType))
						return parentApplied;

					var target = (IList)propertyInfo.GetValue(element.DataContext, null);
					CollectionHelper.Bind(((MultiSelector)element).SelectedItems, target);

					return true;
				};
		}

		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			var viewModelType = viewModel.GetType();
			var elements = view.DeepChildren()
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