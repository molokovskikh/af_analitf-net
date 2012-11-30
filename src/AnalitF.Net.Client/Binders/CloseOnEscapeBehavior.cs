using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Console = System.Console;

namespace AnalitF.Net.Client.Binders
{
	public class CloseOnEscapeBehavior
	{
		public void Attach(UIElement element)
		{
			Observable.FromEventPattern<KeyEventArgs>(element, "KeyDown")
				.Where(e => e.EventArgs.Key == Key.Escape)
				.Subscribe(e => ViewModelHelper.InvokeDataContext(e.Sender, "TryClose"));
		}
	}

}