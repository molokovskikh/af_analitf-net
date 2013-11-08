using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using AnalitF.Net.Test.Integration.Views;
using Common.Tools;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BindingChecker
	{
		public static IDisposable Track()
		{
			ViewSetup.BindingErrors.Clear();
			return Disposable.Create(() => {
				var errors = ViewSetup.BindingErrors.ToArray();
				if (errors.Length > 0) {
					throw new Exception(errors.Implode(Environment.NewLine));
				}
			});
		}
	}
}