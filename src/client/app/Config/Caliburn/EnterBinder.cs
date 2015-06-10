using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Inflector;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public class EnterBinder
	{
		public static void Bind(MethodInfo method, FrameworkElement element)
		{
			var keydown = Observable.FromEventPattern<KeyEventArgs>(element, "KeyDown")
				.Where(a => a.EventArgs.Key == Key.Return
					&& !a.EventArgs.Handled
					&& ((DataGrid)a.Sender).SelectedItem != null)
				.Do(a => a.EventArgs.Handled = true)
				.Select(a => ((DataGrid)a.Sender).SelectedItem);

			var mouseDoubleClick = Observable.FromEventPattern<MouseButtonEventArgs>(element, "MouseDoubleClick")
				.Select(a => ((DependencyObject)a.EventArgs.OriginalSource)
					.Parents<DataGridCell>().FirstOrDefault());

			keydown.Merge(mouseDoubleClick).Where(i => i != null)
				.CatchSubscribe(_ => {
					var context = new ActionExecutionContext {
						Method = method,
						Source = element,
						Message = new ActionMessage {
							MethodName = method.Name,
						}
					};
					ActionMessage.PrepareContext(context);
					ActionMessage.InvokeAction(context);
				});
		}

		public static void Bind(Type type, IEnumerable<FrameworkElement> elements, List<FrameworkElement> binded)
		{
			var pattern = "Enter";
			var methods = type.GetMethods().Where(m => m.Name.StartsWith(pattern));
			foreach (var method in methods) {
				var name = method.Name.Replace(pattern, "").Pluralize();
				var element = elements.FindName(name);
				if (element == null)
					continue;

				Bind(method, element);
				binded.Add(element);
			}
		}
	}
}