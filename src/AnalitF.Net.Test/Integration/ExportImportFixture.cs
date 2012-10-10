using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Models;
using Common.Tools;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;
using NHibernate.Linq;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture]
	public class ExportImportFixture : IntegrationFixture
	{
		private ISession localSession = null;

		[SetUp]
		public void Setup()
		{
			localSession = Client.Config.Initializers.NHibernate.Factory.OpenSession();
			var export = new SchemaExport(Client.Config.Initializers.NHibernate.Configuration);
			export.Drop(false, true);
			export.Create(false, true);
		}

		[Test, Ignore]
		public void Load_data()
		{
			var supplier = TestSupplier.Create();
			CreateSampleCore1(supplier);
			var client = TestClient.CreateNaked();
			Close();

			var exporter = new Exporter(session);
			var files = exporter.Export(client.Users[0].Id);
			Import(files);
		}

		private void Import(List<System.Tuple<string, string[]>> tables)
		{
			foreach (var table in tables) {
				var sql = String.Format("LOAD DATA INFILE '{0}' INTO TABLE {1} ({2})",
					table.Item1,
					Path.GetFileNameWithoutExtension(table.Item1),
					table.Item2.Implode());
				var dbCommand = session.Connection.CreateCommand();
				dbCommand.CommandText = sql;
				dbCommand.ExecuteNonQuery();
			}
		}

		public void CreateSampleCore1(TestSupplier supplier)
		{
			var price = supplier.Prices[0];
			var random = new Random();
			var producers = TestProducer.Queryable.Take(1000).ToList();
			var products = TestProduct.Queryable.Fetch(p => p.CatalogProduct).Where(p => !p.CatalogProduct.Hidden).Take(1000).ToList();

			var maxProducer = producers.Count();
			var maxProduct = products.Count();

			var randomProducts = Generator.Random(maxProduct).Select(i => products.Skip(i).Take(1).First());
			var randomProducers = Generator.Random(maxProducer).Select(i => producers.Skip(i).Take(1).First());

			var synonyms = new List<Tuple<string, TestProduct, string, TestProducer>>();
			var productForCreate = randomProducts.Take(20)
				.Concat(randomProducts.Where(p => p.CatalogProduct.VitallyImportant).Take(7))
				.Concat(randomProducts.Where(p => p.CatalogProduct.MandatoryList).Take(3))
				.Concat(randomProducts.Where(p => p.CatalogProduct.MandatoryList && p.CatalogProduct.VitallyImportant).Take(2))
				.Concat(products.Where(p => products.Count(c => c.CatalogProduct == p.CatalogProduct) > 1).Take(3));

			foreach (var product in productForCreate) {
				var producer = randomProducers.First();
				synonyms.Add(Tuple.Create(product.CatalogProduct.Name, product, producer.Name, producer));
			}

			foreach (var data in synonyms) {
				var productSynonymValue = data.Item1;
				if (price.ProductSynonyms.Any(s => s.Name == productSynonymValue))
					productSynonymValue += " " + random.Next(100000).ToString();
				var producerSynonymValue = data.Item3;
				if (price.ProducerSynonyms.Any(s => s.Name == producerSynonymValue))
					producerSynonymValue += " " + random.Next(10000).ToString();

				var productSynonym = price.AddProductSynonym(productSynonymValue, data.Item2);
				var producerSynonym = price.AddProducerSynonym(producerSynonymValue, data.Item4);
				var core = new TestCore(productSynonym, producerSynonym) {
					Price = price,
					Quantity = random.Next(1, 10 * 1000).ToString(),
					Junk = random.Next(100) < 5,
				};
				core.SaveAndFlush();
				core.AddCost((decimal)(random.NextDouble() * 10000));
				price.Core.Add(core);
				core.SaveAndFlush();
			}
		}
	}
}