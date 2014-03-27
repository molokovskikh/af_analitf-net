using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreatePromotion : ServerFixture
	{
		public TestPromotion Promotion;

		public override void Execute(ISession session)
		{
			var serverUser = User(session);
			var supplier = serverUser.GetActivePrices(session).First().Supplier;
			var catalog = session.Query<TestCore>().First().Product.CatalogProduct;
			Promotion = new TestPromotion {
				Name = "Test",
				Annotation = "Test",
				Status = true,
				Supplier = supplier,
				RegionMask = serverUser.WorkRegionMask,
				Catalogs = {
					catalog
				}
			};
			session.Save(Promotion);
			Promotion.Save(Config.PromotionsPath, "test");
			if (Verbose)
				Console.WriteLine(catalog.Name);
		}
	}
}