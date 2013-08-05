using System;
using System.Linq;
using NHibernate;
using Test.Support;

namespace Test.Data
{
	public class DataMother
	{
		public static TestWaybill CreateWaybill(ISession session, TestUser user)
		{
			var supplier = user.GetActivePricesNaked(session).First().Price.Supplier;
			var log = new TestDocumentLog(supplier, user.AvaliableAddresses[0], "");
			var waybill = new TestWaybill(log);
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "Азарга капли глазные 5мл Фл.-кап. Х1",
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
				Product = "Доксазозин 4мг таб. Х30 (R)",
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
			for (var i = 0; i < 30; i++)
				waybill.Lines.Add(new TestWaybillLine(waybill) {
					Product = "Доксазозин 4мг таб. Х30 (R)",
					Certificates = "РОСС RU.ФМ08.Д38737",
					Period = "01.05.2017",
					Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				});
			return waybill;
		}
	}
}