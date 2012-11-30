using System;
using System.Windows;

namespace AnalitF.Net.Client.Binders
{
	public class ViewModelHelper
	{
		public static object InvokeDataContext(object sender, string method)
		{
			if (method == null)
				return null;

			var element = sender as FrameworkElement;
			if (element == null)
				return null;

			var viewModel = element.DataContext;
			if (viewModel == null)
				return null;

			var methodInfo = viewModel.GetType().GetMethod(method, new Type[0]);
			if (methodInfo != null)
				return methodInfo.Invoke(viewModel, null);
			return null;
		}
	}
}