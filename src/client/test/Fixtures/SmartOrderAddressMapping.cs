using System;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class SmartOrderAddressMapping : ServerFixture
	{
		public override void Execute(ISession session)
		{
			Console.Write("Код для соответствия:");
			var value = Console.ReadLine();
			var user = User(session);
			var priceId = user.Client.Settings.SmartOrderRule.AssortmentPriceCode;
			var intersection = session.Query<TestIntersection>()
				.FirstOrDefault(i => i.Price.Id == priceId && i.Client == user.Client);
			if (intersection == null) {
				intersection = new TestIntersection {
					Price = session.Load<TestPrice>(priceId),
					Client = user.Client,
					Region = user.Client.HomeRegion,
					LegalEntity = user.AvaliableAddresses[0].LegalEntity
				};
				intersection.AddressIntersections.Add(new TestAddressIntersection(user.AvaliableAddresses[0], intersection));
				session.Save(intersection);
				session.SaveEach(intersection.AddressIntersections);
			}
			var addressIntersection = intersection.AddressIntersections.First(a => a.Address == user.AvaliableAddresses[0]);
			addressIntersection.SupplierDeliveryId = value;
		}
	}
}