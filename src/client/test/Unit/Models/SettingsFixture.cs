﻿using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class SettingsFixture
	{
		[Test]
		public void Calculate_price_name()
		{
			var settings = new Settings();
			settings.ShowPriceName = true;
			var price = new Price {
				PriceName = "Базовый", SupplierName = "Протек"
			};
			var prices = new List<Price> { price };
			settings.UpdatePriceNames(prices);
			Assert.That(price.Name, Is.EqualTo("Протек Базовый"));

			settings.ShowPriceName = false;
			settings.UpdatePriceNames(prices);
			Assert.That(price.Name, Is.EqualTo("Протек"));
		}

		[Test]
		public void Client_token()
		{
			var settings = new Settings();
			settings.CheckToken();
			var oldToken = settings.ClientToken;
			Assert.IsNotNullOrEmpty(oldToken);

			settings.ClientToken = "123";
			settings.CheckToken();
			Assert.IsNotNullOrEmpty(oldToken);
			Assert.AreNotEqual(oldToken, settings.ClientToken);
		}
	}
}