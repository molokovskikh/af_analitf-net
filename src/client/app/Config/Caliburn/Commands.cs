using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public static class Commands
	{
		public static RoutedUICommand InvokeViewModel = new RoutedUICommand();
		public static RoutedUICommand NavigateUri = new RoutedUICommand();
		public static RoutedUICommand CleanText = new RoutedUICommand();

		public static void DoInvokeViewModel(object sender, ExecutedRoutedEventArgs e)
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

		public static void DoNavigateUri(object sender, ExecutedRoutedEventArgs e)
		{
			var uri = e.Parameter ?? (e.OriginalSource as Hyperlink)?.NavigateUri;
			if (uri == null)
				return;

			new OpenResult(uri.ToString()).Execute(new ActionExecutionContext());
		}

		public static void Bind(object viewModel, DependencyObject view, object context)
		{
			var uielement =  view as UIElement;
			if (uielement == null)
				return;

			uielement.CommandBindings.Add(new CommandBinding(InvokeViewModel, DoInvokeViewModel, CanInvokeViewModel));
			uielement.CommandBindings.Add(new CommandBinding(NavigateUri, DoNavigateUri));
		}
	}
}