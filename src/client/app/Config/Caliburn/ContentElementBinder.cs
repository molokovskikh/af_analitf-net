using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Config.Caliburn
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

		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			var viewModelType = viewModel.GetType();
			var elements = view.Descendants<FrameworkContentElement>()
				.Where(e => !string.IsNullOrEmpty(e.Name))
				.Distinct()
				.ToList();

			foreach (var element in elements) {
				var convention = ConventionManager.GetElementConvention(element.GetType());
				if(convention == null)
					continue;

				PropertyInfo property;
				Type resultViewModelType;
				var bindPath = NotifyValueSupport.Patch(viewModelType, element.Name, out property, out resultViewModelType);
				if(property == null)
					continue;

				Bind(convention, element, bindPath, property, viewModelType);
			}
		}

		private static bool Bind(ElementConvention convention, FrameworkContentElement element, string bindPath, PropertyInfo property, Type viewModelType)
		{
			var bindableProperty = convention.GetBindableProperty(element);

			if (bindableProperty == null || element.GetBindingExpression(bindableProperty) != null) {
				return false;
			}

			var binding = new Binding(bindPath);

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