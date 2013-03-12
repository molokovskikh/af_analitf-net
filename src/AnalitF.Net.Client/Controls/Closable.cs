using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using Xceed.Wpf.Toolkit;

namespace AnalitF.Net.Client.Controls
{
	public class Closable
	{
		public static DependencyProperty CloseOnClickProperty
			= DependencyProperty.RegisterAttached("CloseOnClickProperty", typeof(bool), typeof(Closable), new PropertyMetadata(false, PropertyChangedCallback));

		private static void PropertyChangedCallback(DependencyObject s, DependencyPropertyChangedEventArgs a)
		{
			var l = ((MenuItem)s).Parents().ToList();
			((MenuItem)s).Click += (sender, args) => {
				((MenuItem)s).RaiseEvent(new RoutedEventArgs(DropDownButton.ClosedEvent));
			};
		}

		public static void SetCloseOnClick(DependencyObject o, bool value)
		{
			o.SetValue(CloseOnClickProperty, value);
		}

		public static bool GetCloseOnClick(DependencyObject o)
		{
			return (bool)o.GetValue(CloseOnClickProperty);
		}
	}
}