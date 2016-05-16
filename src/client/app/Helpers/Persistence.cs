using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views.Orders;
using log4net;
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
				Setter = v => value.Value = (T)ViewPersister.ConvertJsonValue(v, typeof(T)),
			};
		}
	}

	public class ViewPersister
	{
		public Dictionary<DependencyObject, DependencyProperty> persistable =
			new Dictionary<DependencyObject, DependencyProperty>();
		private UserControl view;
		private static ILog log = LogManager.GetLogger(typeof(ViewPersister));

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
			try {
				var model = view.DataContext as BaseScreen;
				if (model == null)
					return;
				foreach (var property in persistable) {
					var prop = property.Value;
					var key = GetKey(property);
					if (!model.Shell.PersistentContext.ContainsKey(key))
						return;
					var value = model.Shell.PersistentContext[key];
					value = ConvertJsonValue(value, prop.PropertyType);
					if (value != null)
						property.Key.SetValue(prop, value);
				}
			} catch(Exception e) {
#if DEBUG
				throw;
#endif
				log.Error("Не удалось восстановить состояние", e);
			}
		}

		public static object ConvertJsonValue(object value, Type targetType)
		{
			if (value == null)
				return null;
			if (targetType.IsInstanceOfType(value))
				return value;
			if (value is JObject)
				return ((JObject)value).ToObject(targetType);
			var converter = TypeDescriptor.GetConverter(targetType);
			if (converter.CanConvertFrom(value.GetType()))
				return converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
			converter = TypeDescriptor.GetConverter(value.GetType());
			if (converter.CanConvertTo(targetType))
				return converter.ConvertTo(value, targetType);

#if DEBUG
			throw new Exception($"Не удалось преобразовать значение '{value}' типа {value.GetType()} в тип {targetType}");
#else
			return null;
#endif
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