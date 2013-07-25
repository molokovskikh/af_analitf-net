using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate;
using Test.Support;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DataMother
	{
		private ISession session;

		public DataMother(ISession session)
		{
			this.session = session;
		}

		public Waybill CreateWaybill(Address address, Settings settings)
		{
			var waybill = new Waybill {
				Address = address,
				WriteTime = DateTime.Now,
				DocumentDate = DateTime.Now,
			};
			waybill.Lines = Enumerable.Range(0, 10).Select(i => new WaybillLine(waybill)).ToList();
			var line = waybill.Lines[0];
			line.Quantity = 10;
			line.Nds = 10;
			line.ProducerCost = 15.13m;
			line.SupplierCostWithoutNds = 18.25m;
			line.SupplierCost = 20.8m;
			waybill.Calculate(settings, settings.Markups, true);
			session.Save(waybill);
			session.Flush();

			return waybill;
		}

		public static TestWaybill CreateWaybill(ISession session, TestUser user)
		{
			return global::Test.Data.DataMother.CreateWaybill(session, user);
		}
	}
}