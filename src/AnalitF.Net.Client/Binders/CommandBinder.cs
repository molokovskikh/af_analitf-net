using System.Windows;
using System.Windows.Input;

namespace AnalitF.Net.Client.Binders
{
	public class CommandBinder
	{
		public static void BindCommand(DependencyObject dependencyObject)
		{
			var uielement =  dependencyObject as UIElement;
			if (uielement == null)
				return;

			uielement.CommandBindings.Add(new CommandBinding(Commands.InvokeViewModel, InvokeViewModel, CanInvokeViewModel));
		}

		private static void InvokeViewModel(object sender, ExecutedRoutedEventArgs e)
		{
			ViewModelHelper.InvokeDataContext(sender, e.Parameter as string);
		}

		private static void CanInvokeViewModel(object sender, CanExecuteRoutedEventArgs e)
		{
			var result = ViewModelHelper.InvokeDataContext(sender, "Can" + e.Parameter)
				?? ViewModelHelper.InvokeDataContext(sender, "get_Can" + e.Parameter);
			if (result is bool)
				e.CanExecute = (bool)result;
			else {
				e.CanExecute = true;
			}
		}
	}
}