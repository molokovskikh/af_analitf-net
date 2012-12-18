using System;
using System.Windows;
using AnalitF.Net.Client.Binders;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture, RequiresSTA]
	public class ViewModelHelperFixture
	{
		private TestViewModel model;
		private FrameworkElement element;

		public class TestViewModel
		{
			public int Count;

			public IResult Test()
			{
				return new ActionResult(() => Count++);
			}

			public bool CanTest()
			{
				return false;
			}
		}

		public class ActionResult : IResult
		{
			private System.Action action;

			public ActionResult(System.Action action)
			{
				this.action = action;
			}

			public void Execute(ActionExecutionContext context)
			{
				action();
				if (Completed != null)
					Completed(this, new ResultCompletionEventArgs());
			}

			public event EventHandler<ResultCompletionEventArgs> Completed;
		}

		[SetUp]
		public void Setup()
		{
			IoC.BuildUp = o => { };
			model = new TestViewModel();
			element = new FrameworkElement();
			element.DataContext = model;
		}

		[Test]
		public void Invoke_result()
		{
			ViewModelHelper.InvokeDataContext(element, "Test");
			Assert.That(model.Count, Is.EqualTo(1));
		}

		[Test]
		public void Return_result()
		{
			var result = ViewModelHelper.InvokeDataContext(element, "CanTest");
			Assert.That(result, Is.EqualTo(false));
		}
	}
}