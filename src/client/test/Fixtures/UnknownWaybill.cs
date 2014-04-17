using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class UnknownWaybill
	{
		public Waybill Waybill;

		public void Execute(ISession session)
		{
			var address = session.Query<Address>().First();
			var settings = session.Query<Settings>().First();

			Waybill = new Waybill(address, session.Query<Supplier>().First());
			Waybill.Lines = Enumerable.Range(0, 10).Select(i => new WaybillLine(Waybill)).ToList();
			var line = Waybill.Lines[0];
			line.Quantity = 10;
			line.Nds = 10;
			line.ProducerCost = 15.13m;
			line.SupplierCostWithoutNds = 18.25m;
			line.SupplierCost = 20.8m;
			Waybill.Calculate(settings);
			session.Save(Waybill);
			session.Flush();
		}
	}
}