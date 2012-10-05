using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Helpers
{
	public class ValueDescription<T>
	{
		public ValueDescription(string name, T value)
		{
			Name = name;
			Value = value;
		}

		public string Name { get; set; }
		public T Value { get; set; }
	}

	public static class DescriptionHelper
	{
		public static List<ValueDescription<T>> ToDescriptions<T>(this Enum value)
		{
			var enumType = typeof(T);
			return Enum.GetValues(enumType)
				.Cast<T>()
				.Select(v => new ValueDescription<T>(GetDescription(enumType, v), v))
				.ToList();
		}

		private static string GetDescription<T>(Type enumType, T v)
		{
			var attributes = enumType.GetField(v.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (attributes.Length == 0)
				return "";
			return ((DescriptionAttribute)attributes[0]).Description;
		}
	}
}