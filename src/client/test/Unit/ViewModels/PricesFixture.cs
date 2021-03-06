﻿using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	public class PricesFixture : BaseUnitFixture
	{
		private PriceViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new PriceViewModel();
			model.Prices.Value.Add(new Price("test1"));
			Activate(model);
		}

		[Test]
		public void Do_not_enter_disabled_price()
		{
			model.Prices.Value[0].Active = false;
			model.CurrentPrice.Value = model.Prices.Value[0];
			model.EnterPrice();
			//проверяем что активная модель не изменилась
			Assert.AreEqual(model, shell.ActiveItem);
		}
	}
}