using AnalitF.Net.Client.ViewModels.Inventory;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class CheckoutFixture
	{
		[Test]
		public void Ckecout()
		{
			var model = new Checkout(530);
			Assert.IsTrue(model.IsValid);
			model.Amount.Value = 200;
			model.CardAmount.Value = 500;
			Assert.IsTrue(model.IsValid);
			Assert.AreEqual(170, model.Change.Value);

			model.Amount.Value = 500;
			model.CardAmount.Value = 600;
			Assert.IsFalse(model.IsValid);
			Assert.AreEqual(570, model.Change.Value);

			model.Amount.Value = 0;
			model.CardAmount.Value = 600;
			Assert.IsFalse(model.IsValid);
			Assert.AreEqual(70, model.Change.Value);

			model.Amount.Value = 5000;
			model.CardAmount.Value = 0;
			Assert.True(model.IsValid);
			Assert.AreEqual(4470, model.Change.Value);

			model.Amount.Value = 5000;
			model.CardAmount.Value = 1;
			Assert.IsFalse(model.IsValid);
			Assert.AreEqual(4471, model.Change.Value);

			model.Amount.Value = 0;
			model.CardAmount.Value = 530;
			Assert.IsTrue(model.IsValid);
			Assert.AreEqual(0, model.Change.Value);
		}
	}
}