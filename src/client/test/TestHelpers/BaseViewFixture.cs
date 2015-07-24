using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Test.Integration.Views;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BaseViewFixture : ViewModelFixture
	{
		[SetUp]
		public void BaseViewFixtureSetup()
		{
			disposable.Add(BindingChecker.Track());
			ViewSetup.Setup();
		}

		public static void ForceBinding(UIElement view)
		{
			var size = new Size(1000, 1000);
			view.Measure(size);
			view.Arrange(new Rect(size));
		}

		protected UserControl Bind(BaseScreen priceViewModel)
		{
			var model = Init(priceViewModel);
			var view = ViewLocator.LocateForModel(model, null, null);
			ViewModelBinder.Bind(model, view, null);
			ForceBinding(view);
			return (UserControl)view;
		}

		protected void UseWindow(BaseScreen model, Func<Window, UserControl, Task> action)
		{
			WpfTestHelper.WithWindow2(async w => {
				var view = Bind(model);
				w.Content = view;

				await w.WaitLoaded();
				await action(w, view);
			});
		}
	}
}