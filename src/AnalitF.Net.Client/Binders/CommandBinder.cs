using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public class CommandBinder
	{
		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			var uielement =  view as UIElement;
			if (uielement == null)
				return;

			uielement.CommandBindings.Add(new CommandBinding(Commands.InvokeViewModel, InvokeViewModel, CanInvokeViewModel));
			uielement.CommandBindings.Add(new CommandBinding(Commands.NavigateUri, NavigateUri));
		}

		private static void NavigateUri(object sender, ExecutedRoutedEventArgs e)
		{
			var uri = e.Parameter ?? (e.OriginalSource is Hyperlink ? ((Hyperlink)e.OriginalSource).NavigateUri : null);
			if (uri == null)
				return;

			new OpenResult(uri.ToString()).Execute(new ActionExecutionContext());
		}

		public static void InvokeViewModel(object sender, ExecutedRoutedEventArgs e)
		{
			ViewModelHelper.InvokeDataContext(sender, e);
		}

		public static void CanInvokeViewModel(object sender, CanExecuteRoutedEventArgs e)
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