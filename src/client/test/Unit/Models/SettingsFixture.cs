using System;
using System.Collections.Generic;
using System.IO;
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
			var crypToken = settings.ClientTokenV2;
			var token = settings.GetClientToken();
			Assert.That(crypToken, Is.Not.Null.Or.Empty);
			Assert.That(settings.GetClientToken(), Is.Not.Null.Or.Empty);

			settings.ClientTokenV2 = "123";
			Assert.IsNull(settings.GetClientToken());
			settings.CheckToken();
			Assert.That(crypToken, Is.Not.Null.Or.Empty);
			Assert.AreNotEqual(crypToken, settings.ClientTokenV2);
			Assert.AreNotEqual(token, settings.GetClientToken());
		}

		[Test]
		public void Change_client_token_on_path_change()
		{
			var settings = new Settings();
			settings.CheckToken();
			var token = settings.GetClientToken(@"C:\af\");
			var token2 = settings.GetClientToken(@"C:\users\test\af\");
			Assert.AreNotEqual(token, token2);
		}

		[Test]
		public void Map_path()
		{
			var settings = new Settings();
			Assert.AreEqual(Path.GetFullPath(@"var\client\АналитФАРМАЦИЯ\Накладные"), settings.MapPath("Waybills"));
			settings.WaybillDir = "";
			Assert.AreEqual(Path.GetFullPath(@"var\client\АналитФАРМАЦИЯ\Накладные"), settings.MapPath("Waybills"));
			settings.WaybillDir = @"C:\";
			Assert.AreEqual(@"C:\", settings.MapPath("Waybills"));
		}
	}
}