using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.NHibernate;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.ViewModels.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class StockAssortmentFixture : ViewModelFixture
	{
		private Waybill Waybill;
		private StockAssortmentViewModel model;
		[SetUp]
		public void Setup()
		{
			session.DeleteEach<Waybill>();
			session.DeleteEach<Stock>();
			Catalog catalog = session.Query<Catalog>().First();
			Waybill = new Waybill(address, session.Query<Supplier>().First());
			Waybill.Lines.Add(new WaybillLine(Waybill)
			{

				ProductId = catalog.Id,
				Product = catalog.FullName,
				Certificates = "РОСС RU.ФМ08.Д38737",
				Period = "01.05.2017",
				Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				ProducerCost = 213.18m,
				RegistryCost = 382.89m,
				SupplierPriceMarkup = -5.746m,
				SupplierCostWithoutNds = 200.93m,
				SupplierCost = 221.03m,
				Quantity = 2,
				VitallyImportant = true,
				Nds = 10,
				SerialNumber = "21012",
				Amount = 442.05m,
				NdsAmount = 40.19m,
				BillOfEntryNumber = "10609010/101209/0004305/1",
				//для отчета по жизененно важным
				EAN13 = "4606915000379",
			});
			session.Save(Waybill);
			foreach (var item in Waybill.Lines)
			{
				item.Stock = new Stock(Waybill, item, session);
				session.Save(item.Stock);
			}
			Waybill.Calculate(settings, new List<uint>());
			session.Save(Waybill);
			session.Flush();
			Waybill.Stock(session);
			model = Open(new StockAssortmentViewModel());
		}

		[Test]

		public void StockAssortment()
		{
			Assert.That(model.Catalogs.Value.Count, Is.GreaterThan(0));
			Assert.That(model.AddressStock.Value.Count, Is.GreaterThan(0));
			Assert.That(model.Stocks.Value.Count, Is.GreaterThan(0));
			Assert.That(model.StockActions.Value.Count, Is.GreaterThan(0));
		}
	}

}