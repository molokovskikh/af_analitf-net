using System;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using AnalitF.Net.Test.Integration.Views;
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
			var view1 = ViewLocator.LocateForModel(model, null, null);
			ViewModelBinder.Bind(model, view1, null);
			var view = view1;
			ForceBinding(view);
			return (UserControl)view;
		}
	}
}