using System;
using System.ComponentModel;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;

namespace AnalitF.Net.Client.Test.Fixtures
{
	[Description("Создает накладную связанную с заказом проверка функционала - сопоставления накладных, должен быть отправленный заказ")]
	public class MatchedWaybill : ServerFixture
	{
		public TestWaybill Waybill;

		public override void Execute(ISession session)
		{
			var user = User(session);
			var order = session.Query<TestOrder>().Where(o => o.User == user)
				.OrderByDescending(o => o.WriteTime)
				.FirstOrDefault();
			if (order == null)
				throw new Exception("Не заказов для формирования накладной");
			var log = new TestDocumentLog(order.Price.Supplier, order.Address);
			Waybill = new TestWaybill(log);
			session.Save(Waybill);
			session.Save(new TestDocumentSendLog(user, log));

			foreach (var orderline in order.Items) {
				var line = new TestWaybillLine(Waybill);
				line.Product = orderline.Product.FullName;
				line.Quantity = orderline.Quantity;
				line.SupplierCost = (decimal?)orderline.Cost;
				Waybill.Lines.Add(line);
				session.Save(line);
				session.CreateSQLQuery("insert into Documents.WaybillOrders(DocumentLineId, OrderLineId) values (:documentLineId, :orderLineId)")
					.SetParameter("documentLineId", line.Id)
					.SetParameter("orderLineId", orderline.Id)
					.ExecuteUpdate();
			}
		}
	}
}