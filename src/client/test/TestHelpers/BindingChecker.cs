using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Test.Integration.Views;
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
				var ignored = new [] {
					"System.Windows.Data Error: 4 : ",
					"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=VerticalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'VerticalContentAlignment' (type 'VerticalAlignment')",
					"Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')"
				};
				errors = errors.Except(ignored).ToArray();
				if (errors.Length > 0) {
					throw new Exception(errors.Implode(Environment.NewLine));
				}
			});
		}
	}
}