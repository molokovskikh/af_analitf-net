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
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;
using DataGrid = System.Windows.Controls.DataGrid;
using Selector = System.Windows.Controls.Primitives.Selector;

namespace AnalitF.Net.Client.UI
{
	public class Conventions
	{
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
					if (((DataGrid)element).Columns.Count > 1)
						Interaction.GetBehaviors(element).Add(new Persistable());

					var fallback = ConventionManager.GetElementConvention(typeof(MultiSelector));
					if (fallback != null) {
						return GuesAlignment(fallback, viewModelType, path, property, element);
					}
					return false;
				};
		}

		private static bool GuesAlignment(ElementConvention fallback, Type viewModelType, string path, PropertyInfo property, FrameworkElement element)
		{
			var result = fallback.ApplyBinding(viewModelType, path, property, element, fallback);
			var dummy = "";
			NotifyValueSupport.Patch(ref dummy, ref property);
			var propertyType = property.PropertyType;
			if (result
				&& propertyType.IsGenericType
				&& typeof(IEnumerable).IsAssignableFrom(propertyType)) {
				var dataGrid = ((DataGrid)element);
				var columns = dataGrid.Columns;
				foreach (var column in columns) {
					if (column.Header is string) {
						column.Header = new TextBlock {
							Text = (string)column.Header,
							ToolTip = column.Header,
						};
					}

					if (column is DataGridTextColumnEx && ((DataGridTextColumnEx)column).Binding is Binding) {
						var exColumn = (DataGridTextColumnEx)column;
						var columnPath = ((Binding)exColumn.Binding).Path.Path;
						var type = propertyType.GetGenericArguments()[0];
						var columnProperty = Util.GetProperty(type, columnPath);
						if (columnProperty == null)
							continue;
						var columnType = columnProperty.PropertyType;
						if (Util.IsNumeric(columnType)) {
							exColumn.TextAlignment = TextAlignment.Right;
						}
						if (Util.IsDateTime(columnType)) {
							exColumn.TextAlignment = TextAlignment.Center;
						}
					}
				}
			}
			return result;
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