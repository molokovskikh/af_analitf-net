using System;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class SimpleFixture
	{
		public static void CreateCleanAwaited(ISession session)
		{
			session.DeleteEach<AwaitedItem>();
			var offer = session.Query<Offer>().First();
			var catalog = session.Load<Catalog>(offer.CatalogId);
			var item = new AwaitedItem(catalog);
			session.Save(item);
		}

		[Description("Создает отправленный заказ без предложения, для тестирования функции 'Показать историю заказов'")]
		public static void SentOrderWithoutOffer(ISession session, bool verbose = false)
		{
			var address = session.Query<Address>().First();
			var offer = session.Query<Offer>().First();
			var order = new Order(address, offer);
			var sent = new SentOrder(order);
			var line = sent.Lines[0];
			var catalog = session.Query<Catalog>().First(c => !c.HaveOffers);
			line.CatalogId  = catalog.Id;
			session.Save(sent);
			if (verbose)
				Console.WriteLine("Создан отправленный заказ для товара {0}", catalog.FullName);
		}
	}
}