using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Test.Unit;
using Caliburn.Micro;
using Common.Tools.Calendar;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class BaseScreenFixture : BaseViewFixture
	{
		private Func<object, DependencyObject, object, UIElement> origin;

		[SetUp]
		public void Setup()
		{
			origin = ViewLocator.LocateForModel;
			ViewLocator.LocateForModel = (model, displayLocation, context) => {
				if (model is TestScreen) {
					var view = new UserControl();
					view.Content = "123546666666666666666666666666666";
					return view;
				}
				if (model is Conductor<IScreen>) {
					var view = new UserControl();
					view.Content = new ContentControl {
						Name = "ActiveItem"
					};
					return view;
				}
				throw new Exception(String.Format("Не знаю как построить view для {0}", model));
			};
		}

		[TearDown]
		public void TearDown()
		{
			ViewLocator.LocateForModel = origin;
		}

		public class TestScreen : BaseScreen
		{
			public int ProcessedCount;

			public void Raise()
			{
				ResultsSink.OnNext(new ViewModelHelperFixture.ActionResult(() => {
					ProcessedCount++;
				}));
			}
		}

		[Test]
		public void Do_not_duplicate_result_subscriptions()
		{
			var shell = new Conductor<IScreen>();
			var model = new TestScreen();
			Init(model);

			WpfTestHelper.WithWindow2(async w => {
				var view = (FrameworkElement)ViewLocator.LocateForModel(shell, null, null);
				ViewModelBinder.Bind(shell, view, null);

				w.Content = view;
				shell.ActiveItem = model;
				await view.WaitLoaded();

				shell.DeactivateItem(model, false);
				await w.WaitIdle();

				shell.ActivateItem(model);
				await w.WaitIdle();

				model.Raise();
				await w.WaitIdle();
			});

			Assert.AreEqual(1, model.ProcessedCount);
		}
	}
}