using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public class SearchBinder
	{
		public static void Bind(Type type, IEnumerable<FrameworkElement> elements, List<FrameworkElement> binded)
		{
			var method = type.GetMethod("Search");
			if (method == null)
				return;

			var element = elements.FindName("SearchText");
			if (element == null)
				return;

			var observable = Observable.FromEventPattern<KeyEventArgs>(element, "KeyDown")
				.Where(a => a.EventArgs.Key == Key.Return);

			EnterBinder.RegisterTrigger(element, method, observable);
		}
	}
}