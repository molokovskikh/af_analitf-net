using System;
using System.Collections.Generic;
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
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture]
	public class ExportImportFixture : IntegrationFixture
	{
		private ISession localSession = null;

		[SetUp]
		public void Setup()
		{
			localSession = SetupFixture.Factory.OpenSession();
			var export = new SchemaExport(SetupFixture.Configuration);
			export.Drop(false, true);
			export.Create(false, true);
		}

		[TearDown]
		public void Teardown()
		{
			localSession.Flush();
			localSession.Dispose();
		}

		[Test, Ignore]
		public void Load_data()
		{
			var supplier = TestSupplier.CreateNaked();
			CreateSampleContactInfo(supplier);
			CreateSampleCore(supplier);
			var maxProducerCosts = CreateMaxProduceCosts(supplier);
			var client = TestClient.CreateNaked();
			Close();

			var exporter = new Exporter(session) {
				MaxProducerCostPriceId = maxProducerCosts.Id,
				MaxProducerCostCostId = maxProducerCosts.Costs[0].Id
			};
			var files = exporter.Export(client.Users[0].Id);

			var importer = new Importer(localSession);
			importer.Import(files);
		}

		private void CreateSampleContactInfo(TestSupplier supplier)
		{
			var price = supplier.RegionalData[0];
			price.SupportPhone = "473-2727092";
			price.ContactInfo = @"р/с 40702810213360100306
в Центрально-Черноземный банк СБ РФ г.Воронеж, к/с 30101810600000000681,
БИК 042007681, ОКПО 4790682, ОКОНХ 71100,

  тел./факс. (0732) 72-70-92 – региональный отдел (многоканальный),
   тел. (0732) 72-70-92 – начальник отдела сбыта (Шунелько Елена Владимировна),
  тел. (0732) 57-28-22 – менеджер по претензиям (Ольга Цепина),
  тел. (0732) 72-70-92 – зав. складом (Круликовский Александр).
  E-mail:
  программист: sia@sia.vrn.ru  (Старов Константин),
  начальник отдела сбыта: nos@sia.vrn.ru(Шунелько Елена Владимировна),
менеджер по претензиям: pretenz@sia.vrn.ru (Цепина Ольга)
  прием и обработка заявок: zakaz@sia.vrn.ru (Белобородова Елена)
  менеджер по маркетингу: market1@sia.vrn.ru (Анучкина Нина)
  отдел сертификации: sertif@sia.vrn.ru (Шишкина Виктория)
  склад: sklad@sia.vrn.ru(Круликовский Александр)
   Уважаемые клиенты,в целях улучшения сотрудничества,условий и времени доставки товара,своевременности обработки претензий,корректного обращения,быстрого реагирования на просьбы,выполнение индивидуальных соглашений и т.п.просьба направлять Ваши предложения по совершенствованию сервиса по адресу:
   pravda_all@mail.ru";

			price.OperativeInfo = @"ДОСТАВКА ПО г.ВОРОНЕЖУ
 ЗАЯВКИ, ПРИНЯТЫЕ ДО 12-00, БУДУТ ДОСТАВЛЕНЫ ПОСЛЕ 15-00
  ЗАЯВКИ, ПРИНЯТЫЕ ДО 18-00 В ПЯТНИЦУ, БУДУТ ДОСТАВЛЕНЫ ПО    ЖЕЛАНИЮ КЛИЕНТА В СУББОТУ С 9-00 ДО 15-00

ВСЕ ЭЛЕКТРОННЫЕ ЗАЯВКИ, ПОЛУЧЕННЫЕ  ДО 18-00 ВОСКРЕСЕНЬЯ, БУДУТ   ДОСТАВЛЕНЫ В ПОНЕДЕЛЬНИК ДО 11-00

ЗАЯВКИ ПРИНИМАЮТСЯ ДО 18-30

ДОСТАВКА ОСУЩЕСТВЛЯЕТСЯ ОДИН РАЗ В ДЕНЬ СОГЛАСНО ГРАФИКУ.

МИНИМАЛЬНАЯ СУММА ЗАЯВКИ ДЛЯ ДОСТАВКИ 3000 руб.";
		}

		private TestPrice CreateMaxProduceCosts(TestSupplier supplier)
		{
			var source = supplier.Prices[0].Core.Where(c => c.Product.CatalogProduct.VitallyImportant);

			var holder = TestSupplier.Create();
			var price = holder.Prices[0];
			var synonyms = source.GroupBy(c => new { c.Product, c.Producer })
				.Select(g => Tuple.Create(g.Key.Product.CatalogProduct.Name, g.Key.Product, g.Key.Producer.Name, g.Key.Producer));
			var random = new Random();

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
				};
				core.SaveAndFlush();
				core.AddCost((decimal)(random.NextDouble() * 10000));
				price.Core.Add(core);
				core.SaveAndFlush();
			}

			price.Costs[0].PriceItem.RowCount = price.Core.Count;
			return price;
		}

		public void CreateSampleCore(TestSupplier supplier)
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

			price.Costs[0].PriceItem.RowCount = price.Core.Count;
		}
	}
}