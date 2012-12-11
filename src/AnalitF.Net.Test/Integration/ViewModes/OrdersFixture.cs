using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class OrdersFixture : BaseFixture
	{
		[Test]
		public void Show()
		{
			Init(new OrdersViewModel());
		}

		[Test]
		public void Load_order_on_open_tab()
		{
			var view = Init(new OrdersViewModel());
			Assert.That(view.SentOrders, Is.Null);
			view.IsSentSelected = true;
			view.IsCurrentSelected = false;
			Assert.That(view.SentOrders, Is.Not.Null);
		}
	}
}