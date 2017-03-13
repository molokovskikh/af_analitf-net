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

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class StockAssortmentFixture : ViewModelFixture
	{
		private Waybill waybill;
		private StockAssortmentViewModel model;

		[SetUp]
		public void Setup()
		{
			waybill = Fixture<LocalWaybill>().Waybill;
			waybill.Stock(session);
			model = Open(new StockAssortmentViewModel());
		}

		[Test, Ignore("Нереализовал")]

		public void Waybill_mark_report_without_NDS()
		{
			Assert.That(model.Catalogs.Value.Count, Is.GreaterThan(0));
			Assert.That(model.AddressStock.Value.Count, Is.GreaterThan(0));
			Assert.That(model.Stocks.Value.Count, Is.GreaterThan(0));
			Assert.That(model.StockActions.Value.Count, Is.GreaterThan(0));
		}
	}
}
