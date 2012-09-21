using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using AnalitF.Net.Client.Extentions;
using Caliburn.Micro;
using ReactiveUI.Blend;

namespace AnalitF.Net.Client.Binders
{
	public class EnterBinder
	{
		public static void Bind(MethodInfo method, FrameworkElement element)
		{
			var keydown = Observable.FromEventPattern<KeyEventArgs>(element, "KeyDown")
				.Where(a => a.EventArgs.Key == Key.Return)
				.Select(a => ((DataGrid)a.Sender).SelectedItem);

			var mouseDoubleClick = Observable.FromEventPattern<MouseButtonEventArgs>(element, "MouseDoubleClick")
				.Select(a => XamlExtentions.Parents((DependencyObject)a.EventArgs.OriginalSource).OfType<DataGridCell>().FirstOrDefault());

			var enterObservable = keydown.Merge(mouseDoubleClick).Where(i => i != null);

			var trigger = new ObservableTrigger {
				Observable = enterObservable
			};
			var action = new ActionMessage { MethodName = method.Name };
			foreach (var parameterInfo in method.GetParameters()) {
				action.Parameters.Add(Parser.CreateParameter(element, parameterInfo.Name));
			}
			trigger.Actions.Add(action);
			var triggers = Interaction.GetTriggers(element);
			triggers.Add(trigger);

			var property = (DependencyProperty)(typeof(Message).GetField("MessageTriggersProperty", BindingFlags.Static | BindingFlags.NonPublic))
				.GetValue(null);
			element.SetValue(property, new[] { trigger });
		}
	}
}