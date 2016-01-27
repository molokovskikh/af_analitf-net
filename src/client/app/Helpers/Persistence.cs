using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views.Orders;
using Newtonsoft.Json.Linq;

namespace AnalitF.Net.Client.Helpers
{
	public interface IPersistable
	{
		ViewPersister Persister { get; }
	}

	public class PersistedValue
	{
		public object DefaultValue;
		public string Key;
		public Func<object> Getter;
		public Action<object> Setter;

		public static PersistedValue Create<T>(NotifyValue<T> value, string key)
		{
			return new PersistedValue {
				DefaultValue = value.Value,
				Key = key,
				Getter = () => value.Value,
				Setter = v => value.Value = (T)v,
			};
		}
	}

	public class ViewPersister
	{
		public Dictionary<DependencyObject, DependencyProperty> persistable =
			new Dictionary<DependencyObject, DependencyProperty>();
		private UserControl view;

		public ViewPersister(UserControl view)
		{
			this.view = view;
#if DEBUG
			if (!(view is IPersistable))
				throw new Exception("Вид должен реализовать интерфейс IPersistable");
#endif
		}

		public void Track(DependencyObject o, DependencyProperty property)
		{
			persistable.Add(o, property);
		}

		public void Restore()
		{
			var model = view.DataContext as BaseScreen;
			if (model == null)
				return;
			foreach (var property in persistable) {
				var prop = property.Value;
				var key = GetKey(property);
				if (!model.Shell.PersistentContext.ContainsKey(key))
					return;
				var value = model.Shell.PersistentContext[key];
				if (prop.IsValidType(value)) {
					property.Key.SetValue(prop, value);
				} else if (value is JObject) {
					property.Key.SetValue(prop, ((JObject)value).ToObject(prop.PropertyType));
				} else {
					if (value != null) {
						var converter = TypeDescriptor.GetConverter(prop.PropertyType);
						if (converter.CanConvertFrom(value.GetType())) {
							property.Key.SetValue(prop, converter.ConvertFrom(null, CultureInfo.InvariantCulture, value));
							continue;
						}
					}
#if DEBUG
					throw new Exception($"Не удалось преобразовать значение '{value}' в тип {prop.PropertyType}");
#endif
				}
			}
		}

		public void Save()
		{
			var model = view.DataContext as BaseScreen;
			if (model == null)
				return;
			foreach (var property in persistable) {
				var prop = property.Value;
				model.Shell.PersistentContext[GetKey(property)] = property.Key.GetValue(prop);
			}
		}

		private string GetKey(KeyValuePair<DependencyObject, DependencyProperty> prop)
		{
			var name = (prop.Key as FrameworkElement)?.Name
				?? (prop.Key as FrameworkContentElement)?.Name;
			if (String.IsNullOrEmpty(name))
				return $"{view.GetType().Name}.{prop.Value.Name}";
			return $"{view.GetType().Name}.{name}.{prop.Value.Name}";
		}
	}
}