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

		public LambdaConverter(Func<T, object> select)
		{
			this.select = select;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
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
			return ((int)value) > 0;
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
			return ((bool)value) ? "+" : "";
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
			return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((Visibility)value) == Visibility.Visible;
		}
	}

	public class BoolToCollapsedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((Visibility)value) == Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
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
}