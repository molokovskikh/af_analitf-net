using System.Collections.Generic;
using System.Reflection;

namespace AnalitF.Net.Client.Helpers
{
	public static class Util
	{
		public static bool IsDigitValue(object o)
		{
			return o is int || o is uint || o is decimal || o is double || o is float;
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
				if (property == null)
					return null;
				value = property.GetValue(value, null);
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
				if (property == null)
					return;

				if (i < parts.Length - 1)
					current = property.GetValue(current, null);
			}

			if (property == null)
				return;
			property.SetValue(current, value, null);
		}
	}
}