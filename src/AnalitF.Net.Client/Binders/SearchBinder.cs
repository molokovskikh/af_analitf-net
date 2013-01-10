using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public class SearchBinder
	{
		public static DependencyProperty GridProperty
			= DependencyProperty.RegisterAttached("Grid", typeof(string), typeof(SearchBinder));

		public static void Bind(Type type, IEnumerable<FrameworkElement> elements, List<FrameworkElement> binded)
		{
			var searchMethod = type.GetMethod("Search");
			if (searchMethod == null)
				return;
			var clearMethod = type.GetMethod("ClearSearch");

			var textBox = elements.FindName("SearchText") as TextBox;
			if (textBox == null)
				return;

			AttachKeyDown(textBox, searchMethod, clearMethod);

			var gridName = GetGrid(textBox);
			if (String.IsNullOrEmpty(gridName))
				return;
			var grid = elements.FindName(gridName) as DataGrid;
			if (grid == null)
				return;
			QuickSearchBehavior.AttachInput(grid, textBox);
			AttachKeyDown(grid, searchMethod, clearMethod);
		}

		private static void AttachKeyDown(FrameworkElement element, MethodInfo searchMethod, MethodInfo clearMethod)
		{
			var observable = Observable.FromEventPattern<KeyEventArgs>(element, "KeyDown");
			var enter = observable
				.Where(a => a.EventArgs.Key == Key.Return);
			EnterBinder.RegisterTrigger(element, searchMethod, enter);
			if (clearMethod != null) {
				var escape = observable
					.Where(a => a.EventArgs.Key == Key.Escape);
				EnterBinder.RegisterTrigger(element, clearMethod, escape);
			}
		}

		public static string GetGrid(DependencyObject d)
		{
			return (string)d.GetValue(GridProperty);
		}

		public static void SetGrid(DependencyObject d, string value)
		{
			d.SetValue(GridProperty, value);
		}
	}
}