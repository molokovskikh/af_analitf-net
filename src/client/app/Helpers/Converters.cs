using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Controls;
using log4net;

namespace AnalitF.Net.Client.Helpers
{
	public class GroupNameConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is GroupHeader)
				return ((GroupHeader)value).Name;
			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class GroupConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is GroupHeader;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class LambdaConverter<T> : IValueConverter
	{
		private Func<T, object> @select;
		private Func<object, T> converter;

		public LambdaConverter(Func<T, object> select, Func<object,T> specificConverter = null )
		{
			this.select = select;
			converter = specificConverter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (converter != null) {
				return @select(converter(value));
			}
			return @select((T)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class IntToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (int)value > 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToMarkerConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? "+" : "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToHiddenConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (Visibility)value == Visibility.Visible;
		}
	}

	public class BoolToCollapsedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (Visibility)value == Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	public class IntToCollapsedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((int)value) > 0? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class InvertConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}
	}

	public class NullableConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var forward = TypeDescriptor.GetConverter(targetType);
			if (value == null || forward.CanConvertFrom(value.GetType()))
				return forward.ConvertFrom(value);
			return TypeDescriptor.GetConverter(value.GetType()).ConvertTo(value, targetType);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return TypeDescriptor.GetConverter(targetType).ConvertFrom(value);
		}
	}

	public class InputConverter : IValueConverter
	{
		public static InputConverter Instance = new InputConverter();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;
			targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
			try {
				return System.Convert.ChangeType(value, targetType, culture);
			}
			catch(FormatException) {
				return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
			}
		}
	}

	public class NumberToRubleWords
	{
		public string Convert(decimal value)
		{
			return Convert(decimal.ToDouble(value));
		}

		public string Convert(double value)
		{
			var banknoteN = Math.Truncate(value);
			var coins = (Math.Round(value - banknoteN, 2, MidpointRounding.AwayFromZero) * 100).ToString();
			var banknoteS = Humanizer.NumberToWordsExtension.ToWords(System.Convert.ToInt32(banknoteN),
				(new CultureInfo("ru-Ru")));

			coins = coins.Length > 1 ? coins : coins + "0";

			return $"{banknoteS} руб. {coins} коп.";
		}
	}
}