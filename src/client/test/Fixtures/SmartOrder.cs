using System;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Windows.Media.Media3D;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Models.Catalogs;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
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

			if (Verbose && ProductIds.Length == 0) {
				ProductIds = session.Query<TestCore>().Select(o => o.Product.Id).Distinct().Take(5).ToArray();
				var root = DataMother.GetRoot();
				var file = Path.Combine(root, "src", "data", "smart-order.txt");
				File.WriteAllText(file, ProductIds.Implode(v => String.Format("{0}|1", ProductIds.IndexOf(v) + 1), Environment.NewLine));
				Console.WriteLine("Соответствие продуктов кодам");
				foreach (var productId in ProductIds) {
					Console.WriteLine("{0} -> {1}", session.Load<TestProduct>(productId).FullName, ProductIds.IndexOf(productId) + 1);
				}
				Console.WriteLine("Тестовая дефектура - {0}", FileHelper.RelativeTo(file, root));
			}

			var supplier = TestSupplier.CreateNaked(session, TestRegion.Inforoom);
			var price = supplier.Prices[0];
			price.PriceType = PriceType.Assortment;
			for(var i = 0; i < ProductIds.Length; i++) {
				var product = session.Load<TestProduct>(ProductIds[i]);
				price.Core.Add(new TestCore(price.AddProductSynonym(product.FullName, product)) {
					Code = (i + 1).ToString(), Period = "", Quantity = "", Product = product
				});
			}
			session.Save(supplier);
			Rule.AssortmentPriceCode = price.Id;
			user.Client.Settings.EnableSmartOrder = true;
			user.Client.Settings.SmartOrderRule = Rule;
			session.Save(user);
		}
	}
}