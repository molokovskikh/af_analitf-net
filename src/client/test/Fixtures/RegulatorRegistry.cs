using System;
using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;
using Test.Support.Suppliers;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class RegulatorRegistry : ServerFixture
	{
		public override void Execute(ISession session)
		{
			var supplier = TestSupplier.CreateNaked(session, TestRegion.Inforoom);
			var data = @"
2	Нафазолин	Нафтизина раствор	Капли назальные	Флакон-капельницы пластиковые	1 мг/мл 15 мл №1	Славянская Аптека ООО
3	Нимесулид	Найз	Таблетки	Упаковка контурная ячейковая	100 мг №20	Dr.Reddy'S Laboratories Ltd
4	Ацетилсалициловая кислота+Кофеин+Парацетамол	Цитрамон П	Таблетки	Упаковка контурная ячейковая	№10	Уралбиофарм ОАО";
			var map = new[] {
				"Code",
				"Note",
				"Doc",
				"Series",
				"Unit",
				"Volume",
				"",
				//ClNm - Производитель
				"CodeCr",
			};
			var producers = session.Query<TestProducer>().Take(10).ToList();
			var products = session.Query<TestProduct>().Fetch(p => p.CatalogProduct).Where(p => !p.CatalogProduct.Hidden).Take(10).ToList();

			var maxProducer = producers.Count();
			var maxProduct = products.Count();

			var randomProducts = Generator.Random(maxProduct).SelectMany(i => products.Skip(i).Take(1));
			var randomProducers = Generator.Random(maxProducer).SelectMany(i => producers.Skip(i).Take(1));
			var price = supplier.Prices[0];
			foreach (var line in data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
				var offer = supplier.AddCore(randomProducts.First(), randomProducers.First());
				price.Core.Add(offer);
				session.Save(offer);
				session.Flush();
				var parts = line.Split('\t');
				var update = parts.Where((p, i) => !String.IsNullOrEmpty(map[i])).Select((p, i) => String.Format("{0} = '{1}'", map[i], p)).Implode();
				session.CreateSQLQuery(String.Format("update Farm.Core0 set {0} where id = {1}", update, offer.Id)).ExecuteUpdate();
			}

			var user = User(session);
			var waybill = new TestWaybill(new TestDocumentLog(supplier, user.AvaliableAddresses[0]));
			waybill.DocumentDate = waybill.DocumentDate.AddDays(-7);
			var waybillLine = waybill.AddLine(price.Core[0].Product, price.Core[0].Producer);
			waybillLine.SupplierCostWithoutNDS = 90;
			session.Save(waybill);
			session.Save(new TestDocumentSendLog(user, waybill.Log));

			session.CreateSQLQuery("delete from Customers.AppConfig").ExecuteUpdate();
			session.CreateSQLQuery("insert into Customers.AppConfig (`key`, `value`) value('RegulatorRegistryPriceId', :id)")
				.SetParameter("id", price.Id)
				.ExecuteUpdate();
		}
	}
}