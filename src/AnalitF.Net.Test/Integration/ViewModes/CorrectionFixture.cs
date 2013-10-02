using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class CorrectionFixture : BaseFixture
	{
		Correction correction;

		[SetUp]
		public void Setup()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();
			var orderLine = order.Lines[0];
			orderLine.Apply(new OrderLineResult {
				Result = LineResultStatus.CostChanged,
				ServerCost = orderLine.Cost + 1,
				ServerQuantity = orderLine.Count
			});

			correction = Init<Correction>();
		}

		[Test]
		public void Correction_report()
		{
		}

		[Test]
		public void Disable_send_as_is()
		{
			Assert.AreEqual(1, correction.Lines.Count);
			Assert.IsTrue(correction.CanSend.Value);
			correction.Lines[0].Order.Send = false;
			Assert.IsFalse(correction.CanSend.Value);
		}
	}
}