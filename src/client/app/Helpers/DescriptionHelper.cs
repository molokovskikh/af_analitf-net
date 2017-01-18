using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

	public class ValueDescription
	{
		public ValueDescription(Enum value)
		{
			Name = DescriptionHelper.GetDescription(value);
			Value = value;
		}

		public ValueDescription(string name, object value)
		{
			Name = name;
			Value = value;
		}

		public string Name { get; set; }
		public object Value { get; set; }
	}

	public static class DescriptionHelper
	{
		public static List<ValueDescription> GetDescriptions(Type type)
		{
			return Enum.GetValues(type)
				.Cast<object>()
				.Select(v => new ValueDescription(GetDescription(type.GetField(v.ToString())), v))
				.ToList();
		}

		public static List<ValueDescription<T>> GetDescriptions<T>()
		{
			var enumType = typeof(T);
			return Enum.GetValues(enumType)
				.Cast<T>()
				.Select(v => new ValueDescription<T>(GetDescription(enumType.GetField(v.ToString())), v))
				.ToList();
		}

		public static string GetDescription(ICustomAttributeProvider provider)
		{
			var attributes = provider.GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (attributes.Length == 0)
				return "";
			return ((DescriptionAttribute)attributes[0]).Description;
		}

		public static string GetDescription(Enum value)
		{
			var fieldInfo = value.GetType().GetField(value.ToString());
			return ((DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false))
				.FirstOrDefault()?.Description;
		}
	}
}