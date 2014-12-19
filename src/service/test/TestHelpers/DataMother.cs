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
			var products = session.Query<TestProduct>().Take(32).ToArray();
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "������ ����� ������� 5�� ��.-���. �1",
				CatalogProduct = products[0],
				Certificates = "���� BE.��11.�06711",
				CertificatesDate = "01.16.2013",
				Period = "30.09.2014",
				Producer = "�����-������ �.�. �.�.",
				Country = "�������",
				SupplierCostWithoutNDS = 536.17m,
				SupplierCost = 589.79m,
				Quantity = 1,
				SerialNumber = "A 565",
				Amount = 589.79m,
				NDS = 10,
				NDSAmount = 53.62m,
			});
			waybill.Lines.Add(new TestWaybillLine(waybill) {
				Product = "���������� 4�� ���. �30 (R)",
				CatalogProduct = products[1],
				Certificates = "���� RU.��08.�38737",
				Period = "01.05.2017",
				Producer = "���-���� ���./������-������ ���, ������",
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
					Product = "���������� 4�� ���. �30 (R)",
					CatalogProduct = products[i + 1],
					Certificates = "���� RU.��08.�38737",
					Period = "01.05.2017",
					Producer = "���-���� ���./������-������ ���, ������",
				});
			return waybill;
		}

		public static void News(ISession session)
		{
			session.CreateSQLQuery("insert into Usersettings.News(PublicationDate, Header, Body, DestinationType)"
				+ " values(:publicationDate, :header, :body, :destinationType)")
				.SetParameter("publicationDate", DateTime.Now)
				.SetParameter("header", "�������� �������")
				.SetParameter("body", "<h1>�����</h1>")
				.SetParameter("destinationType", "1")
				.ExecuteUpdate();
		}
	}
}