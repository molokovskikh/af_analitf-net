using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using Common.Tools;

namespace AnalitF.Net.Client.Binders
{
	public class ViewModelHelper
	{
		public static object InvokeDataContext(object sender, ExecutedRoutedEventArgs args)
		{
			var name = args.Parameter as string;
			var parameters = new object[] { args };

			if (String.IsNullOrEmpty(name))
				name = ((RoutedCommand)args.Command).Name;

			if (String.IsNullOrEmpty(name) && args.Parameter != null) {
				var values = ObjectExtentions.ToDictionary(args.Parameter);
				name = values.GetValueOrDefault("Method") as string;
				parameters = values.Where(k => k.Key != "Method").Select(k => k.Value).ToArray();
			}

			if (String.IsNullOrEmpty(name))
				return null;

			return InvokeDataContext(sender, name, parameters);
		}

		public static object InvokeDataContext(object sender, string method, params object[] args)
		{
			if (method == null)
				return null;

			var element = sender as FrameworkElement;
			if (element == null)
				return null;

			var viewModel = element.DataContext;
			if (viewModel == null)
				return null;

			var type = viewModel.GetType();
			var methodInfo = type.GetMethod(method, args.Select(a => a.GetType()).ToArray());
			if (methodInfo == null) {
				args = new object[0];
				methodInfo = type.GetMethod(method, new Type[0]);
			}
			if (methodInfo != null) {
				var result = methodInfo.Invoke(viewModel, args);

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