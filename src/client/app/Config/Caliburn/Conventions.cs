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
using System.Windows.Interactivity;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;
using DataGrid = System.Windows.Controls.DataGrid;
using Selector = System.Windows.Controls.Primitives.Selector;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public class Conventions
	{
		public static EnumConverter EnumConverterInstance = new EnumConverter();

		public class EnumConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return DescriptionHelper.GetDescription((Enum)value);
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
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

		public static void Register()
		{
			ConventionManager.ApplyValueConverter = ApplyValueConverter;
			ConventionManager.AddElementConvention<SplitButton>(ContentControl.ContentProperty, "DataContext", "Click");
			ConventionManager.AddElementConvention<Run>(Run.TextProperty, "Text", "DataContextChanged");
			ConventionManager.AddElementConvention<IntegerUpDown>(UpDownBase<int?>.ValueProperty, "Value", "ValueChanged");
			ConventionManager.AddElementConvention<FlowDocumentScrollViewer>(FlowDocumentScrollViewer.DocumentProperty, "Document ", "DataContextChanged");
			ConventionManager.AddElementConvention<DocumentViewerBase>(DocumentViewerBase.DocumentProperty, "Document ", "DataContextChanged");
			ConventionManager.AddElementConvention<PasswordBox>(ContentElementBinder.PasswordProperty, "Password", "PasswordChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					//по умолчанию для PasswordBox установлен шрифт times new roman
					//высота поля ввода вычисляется на основе шрифта
					//если расположить TextBox и PasswordBox на одном уровне то разница в высоте будет заметна
					//правим эту кривизну
					((PasswordBox)element).FontFamily = SystemFonts.MessageFontFamily;
					return ConventionManager.SetBindingWithoutBindingOverwrite(viewModelType, path, property, element, convention, convention.GetBindableProperty(element));
				};

			ConventionManager.AddElementConvention<MultiSelector>(ItemsControl.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention)
					=> TryBindSelectedItems(viewModelType, path, property, element, convention,
						((MultiSelector)element).SelectedItems);

			ConventionManager.AddElementConvention<ListBox>(ItemsControl.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention)
					=> TryBindSelectedItems(viewModelType, path, property, element, convention,
						((ListBox)element).SelectedItems);

			ConventionManager.AddElementConvention<ComboBox>(ItemsControl.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					NotifyValueSupport.Patch(ref path, ref property);
					if (property.PropertyType.IsEnum) {
						if (NotBindedAndNull(element, ItemsControl.ItemsSourceProperty)
							&& !ConventionManager.HasBinding(element, Selector.SelectedItemProperty)) {

							var items = DescriptionHelper.GetDescriptions(property.PropertyType);
							element.SetValue(ItemsControl.DisplayMemberPathProperty, "Name");
							element.SetValue(ItemsControl.ItemsSourceProperty, items);

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
			ConventionManager.AddElementConvention<DataGrid>(ItemsControl.ItemsSourceProperty, "SelectedItem", "SelectionChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					var dataGrid = (DataGrid)element;
					if (dataGrid.Columns.Count > 1)
						Interaction.GetBehaviors(element).Add(new Persistable());

					var fallback = ConventionManager.GetElementConvention(typeof(MultiSelector));
					if (fallback != null) {
						var actualProperty = property;
						var result = fallback.ApplyBinding(viewModelType, path, actualProperty, element, fallback);
						var dummy = "";
						NotifyValueSupport.Patch(ref dummy, ref actualProperty);
						var propertyType = actualProperty.PropertyType;
						if (result
							&& propertyType.IsGenericType
							&& typeof(IEnumerable).IsAssignableFrom(propertyType)) {
							ConfigureDataGrid(dataGrid, propertyType.GetGenericArguments()[0]);
						}
						return result;
					}
					return false;
				};
			ConventionManager.AddElementConvention<Label>(ContentControl.ContentProperty, "Content", "DataContextChanged")
				.ApplyBinding = (viewModelType, path, property, element, convention) => {
					return ConventionManager.SetBindingWithoutBindingOverwrite(viewModelType, path, property, element, convention, convention.GetBindableProperty(element));
				};
		}

		private static void ApplyValueConverter(Binding binding, DependencyProperty targetProperty, PropertyInfo sourceProperty)
		{
			if (targetProperty == UIElement.VisibilityProperty && typeof (bool).IsAssignableFrom(sourceProperty.PropertyType)) {
				binding.Converter = ConventionManager.BooleanToVisibilityConverter;
				return;
			}

			if (targetProperty == ContentControl.ContentProperty && sourceProperty.PropertyType.IsEnum) {
				binding.Converter = EnumConverterInstance;
			}
		}


		public static void ConfigureDataGrid(DataGrid dataGrid, Type type)
		{
			var columns = dataGrid.Columns;
			foreach (var column in columns) {
				if (column.Header is string) {
					column.Header = new TextBlock {
						Text = (string)column.Header,
						ToolTip = column.Header,
					};
				}

				var boundColumn = column as DataGridBoundColumn;
				if (boundColumn != null && boundColumn.Binding is Binding) {
					var columnPath = ((Binding)boundColumn.Binding).Path.Path;
					var columnProperty = Util.GetProperty(type, columnPath);
					if (columnProperty == null)
						continue;
					var columnType = columnProperty.PropertyType;
					if (boundColumn.Binding.StringFormat == null)
					if (columnType == typeof(decimal) || columnType == typeof(decimal?)) {
						boundColumn.Binding.StringFormat = "0.00";
					}

					var exColumn = column as DataGridTextColumnEx;
					if (exColumn != null) {
						if (Util.IsNumeric(columnType)) {
							exColumn.TextAlignment = TextAlignment.Right;
						}
						if (Util.IsDateTime(columnType)) {
							exColumn.TextAlignment = TextAlignment.Center;
						}
					}
				}
			}
		}

		private static bool TryBindSelectedItems(Type viewModelType,
			string path,
			PropertyInfo property,
			FrameworkElement element,
			ElementConvention convention,
			IList selectedItems)
		{
			var parentApplied = ConventionManager.GetElementConvention(typeof(Selector))
				.ApplyBinding(viewModelType, path, property, element, convention);
			var index = path.LastIndexOf('.');
			index = index == -1 ? 0 : index + 1;
			var baseName = path.Substring(index);
			var propertyInfo = viewModelType.GetPropertyCaseInsensitive("Selected" + baseName);

			if (propertyInfo == null || !typeof(IList).IsAssignableFrom(propertyInfo.PropertyType)) {
				return parentApplied;
			}

			var target = (IList)propertyInfo.GetValue(element.DataContext, null);
			CollectionHelper.Bind(selectedItems, target);
			return true;
		}

		private static bool NotBindedAndNull(FrameworkElement element, DependencyProperty property)
		{
			return !ConventionManager.HasBinding(element, property)
				&& element.GetValue(property) == null;
		}
	}
}