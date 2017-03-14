using System;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Service.Test.TestHelpers
{
	public class DataMother
	{
		public static TestWaybill CreateWaybill(ISession session, TestUser user)
		{
			var supplier = user.GetActivePricesNaked(session).First().Price.Supplier;
			var log = new TestDocumentLog(supplier, user.AvaliableAddresses[0], "");
			var waybill = new TestWaybill(log);
			var products = session.Query<TestProduct>().Where(x => !x.Hidden).Take(32).ToArray();
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = products[0].FullName,
				CatalogProduct = products[0],
				Certificates = "РОСС BE.ФМ11.Д06711",
				CertificatesDate = "01.16.2013",
				Period = "30.09.2014",
				Producer = "Алкон-Куврер н.в. с.а.",
				Country = "БЕЛЬГИЯ",
				SupplierCostWithoutNDS = 536.17m,
				SupplierCost = 589.79m,
				Quantity = 1,
				SerialNumber = "A 565",
				Amount = 589.79m,
				NDS = 10,
				NDSAmount = 53.62m,
			});
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = products[1].FullName,
				CatalogProduct = products[1],
				Certificates = "РОСС RU.ФМ08.Д38737",
				Period = "01.05.2017",
				Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				ProducerCost = 213.18m,
				RegistryCost = 382.89m,
				SupplierPriceMarkup = -5.746m,
				SupplierCostWithoutNDS = 200.93m,
				SupplierCost = 221.03m,
				Quantity = 2,
				VitallyImportant = true,
				NDS = 10,
				SerialNumber = "21012",
				Amount = 442.05m,
				NDSAmount = 40.19m,
				BillOfEntryNumber = "10609010/101209/0004305/1",
				EAN13 = "4605635002748",
			});
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = products[1].FullName,
				CatalogProduct = products[1],
				Certificates = "РОСС RU.ФМ08.Д38737",
				Period = "01.05.2017",
				Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				ProducerCost = 213.18m,
				RegistryCost = 382.89m,
				SupplierPriceMarkup = -5.746m,
				SupplierCostWithoutNDS = 200.93m,
				SupplierCost = 221.03m,
				Quantity = 2,
				VitallyImportant = true,
				NDS = 10,
				SerialNumber = "21012",
				Amount = 442.05m,
				NDSAmount = 40.19m,
				BillOfEntryNumber = "10609010/101209/0004305/1",
				//для отчета по жизененно важным
				EAN13 = "4606915000379",
			});
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Лопедиум капсулы 2 мг",
				CatalogProduct = products[1],
				Period = "01.05.2018",
				Producer = "Салютас Фарма",
				SupplierCostWithoutNDS = 23.5m,
				SupplierCost = 25.86m,
				Quantity = 2,
				SerialNumber = "DR5963",
				EAN13 = "4030855000890",
			});
			for (var i = 0; i < 29; i++)
				waybill.Lines.Add(new TestWaybillLine(waybill) {
					Product = products[i + 1].FullName,
					CatalogProduct = products[i + 1],
					Certificates = "РОСС RU.ФМ08.Д38737",
					Period = "01.05.2017",
					Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
					SupplierCostWithoutNDS = 23.5m,
					SupplierCost = 25.86m,
					Quantity = 2,
				});
			return waybill;
		}

		public static void CreateNews(ISession session)
		{
			session.CreateSQLQuery("insert into Usersettings.News(PublicationDate, Header, Body, DestinationType)"
				+ " values(:publicationDate, :header, :body, :destinationType)")
				.SetParameter("publicationDate", DateTime.Now)
				.SetParameter("header", "Тестовая новость")
				.SetParameter("body", "<h1>Тесто</h1>")
				.SetParameter("destinationType", "1")
				.ExecuteUpdate();
		}

		public static TestProducerPromotion CreateProducerPromotion(ISession session, TestUser user)
		{
			var suppliers = user.GetActivePricesNaked(session).Take(5).Select(x=>x.Price.Supplier);
			var products = session.Query<TestCatalogProduct>().ToList()
											.Where(x=>x.Name.Contains("П"))
											.OrderByDescending(x => x.Name)
											.Take(5).ToArray();
			var producer = session.Query<TestProducer>().First();

			TestProducerPromotion testProducerPromotion = new TestProducerPromotion()
			{
				Name = "TestPromotion",
				Annotation = "Test Producer Promotion OFF 25%",
				Catalogs = products.ToList(),
				Suppliers = suppliers.ToList(),
				Producer = producer,
				Enabled = 1,
				Status = 1,
				AgencyDisabled =1,
				Begin = DateTime.Now.AddMonths(-1),
				End = DateTime.Now.AddMonths(1),
				RegionMask = 0,
				UpdateTime = DateTime.Now
			};

			return testProducerPromotion;
		}


	}
}