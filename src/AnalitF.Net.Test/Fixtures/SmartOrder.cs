using AnalitF.Net.Client.Models;
using NHibernate;
using Test.Support;
using Test.Support.Suppliers;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class SmartOrder : ServerFixture
	{
		public uint[] ProductIds = new uint[0];
		public TestSmartOrderRule Rule;

		public SmartOrder()
		{
			Rule = new TestSmartOrderRule {
				ParseAlgorithm = "TextSource",
				CodeColumn = "0",
				QuantityColumn = "1",
				ColumnSeparator = "|"
			};
		}

		public override void Execute(ISession session)
		{
			var user = User(session);
			var supplier = TestSupplier.CreateNaked(session, 524288);
			var price = supplier.Prices[0];
			price.PriceType = PriceType.Assortment;
			for(var i = 0; i < ProductIds.Length; i++) {
				var product = session.Load<TestProduct>(ProductIds[0]);
				price.Core.Add(new TestCore { Code = (i + 1).ToString(), Period = "", Quantity = "", Product = product });
			}
			session.Save(supplier);
			Rule.AssortmentPriceCode = price.Id;
			user.Client.Settings.EnableSmartOrder = true;
			user.Client.Settings.SmartOrderRule = Rule;
			session.Save(user);
		}
	}
}