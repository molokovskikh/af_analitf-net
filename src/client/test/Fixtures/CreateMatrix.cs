using System;
using System.Linq;
using Castle.ActiveRecord.Framework;
using log4net.Config;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using NPOI.SS.Util;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateMatrix : ServerFixture
	{
		public uint[] Denied = new uint[0];
		public uint[] Warning = new uint[0];

		public override void Execute(ISession session)
		{
			var user = User(session);

			if (Verbose && Denied.Length == 0 && Warning.Length == 0) {
				var price = user.GetActivePricesNaked(session).First(p => p.PositionCount > 1);
				var offer1 = price.Price.Core[0];
				var offer2 = price.Price.Core.First(c => c.Product != offer1.Product);
				Console.WriteLine("Запрет заказа {0}", offer1.Product.FullName);
				Console.WriteLine("Предупреждение при заказе {0}", offer2.Product.FullName);
				Denied = new[] {
					offer1.Product.Id
				};
				Warning = new[] {
					offer2.Product.Id
				};
			}

			var supplier = TestSupplier.CreateNaked(session, TestRegion.Inforoom);
			var priceDenied = supplier.Prices[0];
			priceDenied.PriceType = PriceType.Assortment;
			priceDenied.Matrix = new TestMatrix();

			var warnPrice = new TestPrice(supplier, PriceType.Assortment);
			warnPrice.Matrix = new TestMatrix();
			supplier.Prices.Add(warnPrice);
			session.Save(supplier);
			session.Flush();

			foreach (var productId in Denied) {
				session.Save(new TestBuyingMatrix(priceDenied, session.Load<TestProduct>(productId)));
			}

			foreach (var productId in Warning) {
				session.Save(new TestBuyingMatrix(warnPrice, session.Load<TestProduct>(productId)));
			}

			var settings = user.Client.Settings;

			settings.BuyingMatrix = priceDenied.Matrix;
			settings.BuyingMatrixAction = TestMatrixAction.Block;
			settings.BuyingMatrixType = TestMatrixType.BlackList;
			settings.BuyingMatrixPriceId = priceDenied.Id;

			settings.OfferMatrix = warnPrice.Matrix;
			settings.OfferMatrixAction = TestMatrixAction.Warning;
			settings.OfferMatrixType = TestMatrixType.BlackList;
			settings.OfferMatrixPriceId = warnPrice.Id;

			session.Save(settings.BuyingMatrix);
			session.Save(settings.OfferMatrix);
		}

		public override void Rollback(ISession session)
		{
			var user = User(session);
			var settings = user.Client.Settings;
			settings.BuyingMatrix = null;
			settings.BuyingMatrixPriceId = null;

			settings.OfferMatrix = null;
			settings.OfferMatrixPriceId = null;
		}
	}
}