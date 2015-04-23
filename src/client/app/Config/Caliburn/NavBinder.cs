using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public class NavBinder
	{
		public static DependencyProperty PrevProperty
			= DependencyProperty.RegisterAttached("Prev", typeof(UIElement), typeof(NavBinder));

		public static DependencyProperty NextProperty
			= DependencyProperty.RegisterAttached("Next", typeof(UIElement), typeof(NavBinder));

		public static void Bind(Type type, IEnumerable<FrameworkElement> elements, List<FrameworkElement> binded)
		{
			foreach (var element in binded) {
				if (element.GetValue(PrevProperty) != null
					|| element.GetValue(NextProperty) != null) {
					element.KeyDown += KeyDown;
				}
			}
		}

		private static void KeyDown(object sender, KeyEventArgs args)
		{
			var el = (FrameworkElement)sender;
			if (args.Key == Key.Escape) {
				var prev = GetPrev(el);
				if (prev != null) {
					args.Handled = prev is DataGrid ? DataGridHelper.Focus((DataGrid)prev) : prev.Focus();
				}
			}

			if (args.Key == Key.Enter) {
				var next = GetNext(el);
				if (next != null) {
					args.Handled = next is DataGrid ? DataGridHelper.Focus((DataGrid)next) : next.Focus();
				}
			}
		}

		public static UIElement GetPrev(DependencyObject d)
		{
			return (UIElement)d.GetValue(PrevProperty);
		}

		public static void SetPrev(DependencyObject d, UIElement value)
		{
			d.SetValue(PrevProperty, value);
		}

		public static UIElement GetNext(DependencyObject d)
		{
			return (UIElement)d.GetValue(NextProperty);
		}

		public static void SetNext(DependencyObject d, UIElement value)
		{
			d.SetValue(NextProperty, value);
		}

	}
}