using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using AnalitF.Net.Test.Integration.Views;
using Common.Tools;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BindingChecker
	{
		public static List<string> BindingErrors = new List<string>();

		public static IDisposable Track()
		{
			BindingErrors.Clear();
			return Disposable.Create(() => {
				BindingErrors.Clear();
				var errors = BindingErrors.ToArray();
				if (errors.Length > 0) {
					throw new Exception(errors.Implode(Environment.NewLine));
				}
			});
		}
	}
}