using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Helpers
{
	public static class Util
	{
		public static bool IsNumeric(object o)
		{
			return o is short || o is ushort
				|| o is int || o is uint
				|| o is long || o is ulong
				|| o is byte
				|| o is decimal
				|| o is double
				|| o is float;
		}

		public static bool IsDateTime(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			return type == typeof(DateTime);
		}

		public static bool IsNumeric(this Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			return type == typeof(short) || type == typeof(ushort)
				|| type == typeof(int) || type == typeof(uint)
				|| type == typeof(long) || type == typeof(ulong)
				|| type == typeof(byte)
				|| type == typeof(decimal)
				|| type == typeof(double)
				|| type == typeof(float);
		}

		public static PropertyInfo GetProperty(Type type, string path)
		{
			if (type == null)
				return null;
			var parts = path.Split('.');
			PropertyInfo property = null;
			foreach (var part in parts) {
				property = type.GetProperty(part);
				if (property == null)
					return null;
				type = property.PropertyType;
			}
			return property;
		}

		public static object GetValue(object item, string path)
		{
			var parts = path.Split('.');

			var value = item;
			foreach (var part in parts) {
				if (value == null)
					return null;
				var type = value.GetType();
				var property = type.GetProperty(part);
				if (property != null) {
					value = property.GetValue(value, null);
				}
				else {
					var field = type.GetField(part);
					if (field == null)
						return null;
					value = field.GetValue(value);
				}
			}
			return value;
		}

		public static void SetValue(object item, string path, object value)
		{
			var parts = path.Split('.');

			var current = item;
			PropertyInfo property = null;
			for(var i = 0; i < parts.Length; i++) {
				if (current == null)
					return;
				var type = current.GetType();
				property = type.GetProperty(parts[i]);
				if (property != null) {
					if (i < parts.Length - 1)
						current = property.GetValue(current, null);
					else if (property.CanWrite)
						property.SetValue(current, value, null);
				}
				else {
					var field = type.GetField(parts[i]);
					if (field == null)
						return;
					if (i < parts.Length - 1)
						current = field.GetValue(current);
					else
						field.SetValue(current, value);
				}
			}
		}

		public static string HumanizeSize(long size, string zero = "")
		{
			if (size == 0)
				return "-";
			if (size < 1024)
				return size + " Б";
			if (size < 1048576)
				return (size / 1024f).ToString("#.##") + " КБ";
			if (size < 1073741824)
				return (size / 1048576f).ToString("#.##") + " МБ";
			return (size / 1073741824f).ToString("#.##") + " ГБ";
		}

		public static void Wait(Func<bool> func, TimeSpan timeout, string message = null)
		{
			var elapsed = new TimeSpan();
			var wait = TimeSpan.FromMilliseconds(100);
			while (func()) {
				Thread.Sleep(wait);
				elapsed += wait;
				if (elapsed > timeout)
					throw new Exception(String.Format("Не удалось дождаться за {0} {1}", timeout, message));
			}
		}
	}
}