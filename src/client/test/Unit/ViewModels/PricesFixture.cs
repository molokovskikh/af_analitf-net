using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	public class PricesFixture : BaseUnitFixture
	{
		private PriceViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new PriceViewModel();
			model.Prices.Add(new Price("test1"));
			Activate(model);
		}

		[Test]
		public void Do_not_enter_disabled_price()
		{
			model.Prices[0].Active = false;
			model.CurrentPrice.Value = model.Prices[0];
			model.EnterPrice();
			//проверяем что активная модель не изменилась
			Assert.AreEqual(model, shell.ActiveItem);
		}
	}
}