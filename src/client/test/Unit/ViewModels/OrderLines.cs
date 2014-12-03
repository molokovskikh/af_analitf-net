using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	public class OrderLines : BaseUnitFixture
	{
		private OrderLinesViewModel model;

		[SetUp]
		public void SetUp()
		{
			model = new OrderLinesViewModel();
			Activate(model);
		}

		[Test]
		public void Update_calendar_on_session_load()
		{
			model.IsSentSelected.Value = true;
			model.IsCurrentSelected.Value = false;
			ScreenExtensions.TryDeactivate(model, true);
			model = new OrderLinesViewModel();
			Activate(model);
			Assert.IsTrue(model.IsSentSelected.Value);
			Assert.IsTrue(model.EndEnabled.Value);
			Assert.IsTrue(model.BeginEnabled.Value);
		}
	}
}