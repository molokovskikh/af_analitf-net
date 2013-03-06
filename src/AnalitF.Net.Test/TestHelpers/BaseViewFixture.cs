using System;
using System.Windows;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Integration.ViewModes;
using AnalitF.Net.Test.Integration.Views;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class BaseViewFixture : BaseFixture
	{
		[SetUp]
		public void BaseViewFixtureSetup()
		{
			ViewFixtureSetup.BindingErrors.Clear();
			ViewFixtureSetup.Setup();
		}

		[TearDown]
		public void TearDown()
		{
			if (ViewFixtureSetup.BindingErrors.Count > 0) {
				throw new Exception(ViewFixtureSetup.BindingErrors.Implode());
			}
		}

		protected T InitView<T>(BaseScreen model) where T : DependencyObject, new()
		{
			var view = ViewLocator.LocateForModel(model, null, null);
			ViewModelBinder.Bind(model, view, null);
			return view as T;
		}

		protected UIElement InitView(BaseScreen model)
		{
			var view = ViewLocator.LocateForModel(model, null, null);
			ViewModelBinder.Bind(model, view, null);
			return view;
		}

		public static void ForceBinding(UIElement view)
		{
			var size = new Size(1000, 1000);
			view.Measure(size);
			view.Arrange(new Rect(size));
		}
	}
}