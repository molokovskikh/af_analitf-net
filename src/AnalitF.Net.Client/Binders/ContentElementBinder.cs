using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Common.Tools;
using Inflector;
using NHibernate.Bytecode.Lightweight;
using NHibernate.Mapping;
using ReactiveUI;
using Xceed.Wpf.Toolkit;
using DataGrid = System.Windows.Controls.DataGrid;

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
				return s.Singularize() ?? s;
			};
			ConventionManager.AddElementConvention<SplitButton>(SplitButton.ContentProperty, "DataContext", "Click");
			ConventionManager.AddElementConvention<Run>(Run.TextProperty, "Text", "DataContextChanged");
			ConventionManager.AddElementConvention<IntegerUpDown>(IntegerUpDown.ValueProperty, "Value", "ValueChanged");
			ConventionManager.AddElementConvention<FlowDocumentScrollViewer>(FlowDocumentScrollViewer.DocumentProperty, "Document ", "DataContextChanged");
			ConventionManager.AddElementConvention<DocumentViewerBase>(DocumentViewerBase.DocumentProperty, "Document ", "DataContextChanged");
			ConventionManager.AddElementConvention<PasswordBox>(PasswordProperty, "Password", "PasswordChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					((PasswordBox)element).FontFamily = SystemFonts.MessageFontFamily;
					return ConventionManager.SetBindingWithoutBindingOverwrite(viewModelType, path, property, element, convention, convention.GetBindableProperty(element));
				};
			ConventionManager.AddElementConvention<MultiSelector>(Selector.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
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
			ConventionManager.AddElementConvention<ComboBox>(Selector.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					NotifyValueSupport.Patch(ref path, ref property);
					if (property.PropertyType.IsEnum) {
						if (NotBindedAndNull(element, Selector.ItemsSourceProperty)
							&& !ConventionManager.HasBinding(element, Selector.SelectedItemProperty)) {

							var items = DescriptionHelper.GetDescription(property.PropertyType);
							element.SetValue(Selector.DisplayMemberPathProperty, "Name");
							element.SetValue(Selector.ItemsSourceProperty, items);

							var binding = new Binding(path);
							binding.Converter = new ComboBoxSelectedItemConverter();
							binding.ConverterParameter = items;
							BindingOperations.SetBinding(element, Selector.SelectedItemProperty, binding);
						}
					}
					else {
						var fallback = ConventionManager.GetElementConvention(typeof(Selector));
						if (fallback != null) {
							return fallback.ApplyBinding(viewModelType, path, property, element, fallback);
						}
					}
					return true;
				};
			ConventionManager.AddElementConvention<DataGrid>(Selector.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					var fallback = ConventionManager.GetElementConvention(typeof(MultiSelector));
					if (fallback != null) {
						var result = fallback.ApplyBinding(viewModelType, path, property, element, fallback);
						if (result
							&& property.PropertyType.IsGenericType
							&& typeof(IList).IsAssignableFrom(property.PropertyType)) {
							var columns = ((DataGrid)element).Columns.OfType<DataGridTextColumnEx>();
							foreach (var column in columns) {
								if (column.Binding is Binding) {
									var columnPath = ((Binding)column.Binding).Path.Path;
									var type = property.PropertyType.GetGenericArguments()[0];
									var columnProperty = Util.GetProperty(type, columnPath);
									if (columnProperty == null)
										continue;
									var columnType = columnProperty.PropertyType;
									if (Util.IsNumeric(columnType)) {
										column.TextAlignment = TextAlignment.Right;
									}
									if (Util.IsDateTime(columnType)) {
										column.TextAlignment = TextAlignment.Center;
									}
								}
							}
						}
						return result;
					}
					return false;
				};
		}

		public class ComboBoxSelectedItemConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return ((IEnumerable<ValueDescription>)parameter).FirstOrDefault(d => Equals(d.Value, value));
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return ((ValueDescription)value).Value;
			}
		}

		private static bool NotBindedAndNull(FrameworkElement element, DependencyProperty property)
		{
			return !ConventionManager.HasBinding(element, property)
				&& element.GetValue(property) == null;
		}

		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			var viewModelType = viewModel.GetType();
			var elements = view.Descendants<FrameworkContentElement>()
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
					continue;
				}

				var convention = ConventionManager.GetElementConvention(element.GetType());
				if(convention == null) {
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