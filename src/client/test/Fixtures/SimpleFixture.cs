using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Tasks;
using Common.Models;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Address = AnalitF.Net.Client.Models.Address;
using Offer = AnalitF.Net.Client.Models.Offer;
using Order = AnalitF.Net.Client.Models.Order;

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

		[Service]
		public static void SmartOrderSetLimit(ISession session)
		{
			Console.Write("Поставщик:");
			var supplierId = Convert.ToUInt32(Console.ReadLine());
			Console.Write("Лимит:");
			var value = Convert.ToDecimal(Console.ReadLine());
			var user = ServerFixture.User(session);
			var address = session.Load<Common.Models.Address>(user.AvaliableAddresses[0].Id);
			var limit = address.SmartOrderLimits.FirstOrDefault(l => l.Supplier.Id == supplierId);
			if (limit == null) {
				limit = new SmartOrderLimit(session.Load<Common.Models.Supplier>(supplierId), value);
				address.SmartOrderLimits.Add(limit);
			}
			limit.Value = value;
			session.Save(address);
		}

		[Description("Создает адрес доставки, на сервере"), Service]
		public static void ServiceAddress(ISession session)
		{
			var user = ServerFixture.User(session);
			var address = user.Client.CreateAddress();
			session.Save(address);
			address.Value += " " + address.Id;
			user.AvaliableAddresses.Add(address);
		}
	}
}