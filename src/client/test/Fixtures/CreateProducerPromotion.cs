using System;
using System.Linq;
using System.ComponentModel;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Test.Fixtures
{

	[Description("Создает тестовую промоакцию производителя на сервере")]
	public class CreateProducerPromotion : ServerFixture
	{
		public TestProducerPromotion ProducerPromotion;

		public override void Execute(ISession session)
		{

			var producerUser = GetProducerUser();
			session.Save(producerUser);

			var serverUser = User(session);
			ProducerPromotion = GetProducerPromotion(session, serverUser);

			ProducerPromotion.ProducerUserId = producerUser.Id;

			session.Save(ProducerPromotion);
			ProducerPromotion.Save(Config.ProducerPromotionsPath, "test");

			if (Verbose)
			{
				Console.WriteLine("Создана тестовая промоакция производителя, препараты: ");
				foreach (var CatalogItem in ProducerPromotion.Catalogs)
				{
					Console.WriteLine("Препарат: {0}", CatalogItem.Name);
				}
			}

		}

		private TestProducerPromotion GetProducerPromotion(ISession session, TestUser user)
		{
			var suppliers = user.GetActivePricesNaked(session).Take(5).Select(x => x.Price.Supplier).ToList();
			var product = session.Query<TestCore>().ToList().Skip(3).First().Product.CatalogProduct;
			var producer = session.Query<TestProducer>().First();

			TestProducerPromotion testProducerPromotion = new TestProducerPromotion()
			{
				Name = "Тестовая промоакция производителя",
				Annotation = "Аннотация тестовой акции производителя",
				Suppliers = suppliers,
				Producer = producer,
				Catalogs = new List<TestCatalogProduct>() { product }.ToList(),
				Enabled = 1,
				Status = 1,
				AgencyDisabled = 1,
				Begin = DateTime.Now.AddMonths(-1),
				End = DateTime.Now.AddMonths(1),
				RegionMask = 0,
				UpdateTime = DateTime.Now
			};

			return testProducerPromotion;
		}

		private TestProducerUser GetProducerUser()
		{
			return new TestProducerUser()
			{
				 Login = "Тестовый пользователь от производителя",
				 TypeUser = 0
			};
		}
	}
}
