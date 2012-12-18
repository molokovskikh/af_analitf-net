using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;

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
			if (methodInfo != null) {
				var result = methodInfo.Invoke(viewModel, null);

				IEnumerator<IResult> actions = null;

				if (result is IResult) {
					result = new [] { (IResult)result };
				}

				if (result is IEnumerable<IResult>) {
					result = ((IEnumerable<IResult>)result).GetEnumerator();
				}

				if (result is IEnumerator<IResult>) {
					actions = result as IEnumerator<IResult>;
				}

				if (actions != null)
					Coroutine.BeginExecute(actions);
				return result;
			}
			return null;
		}
	}
}