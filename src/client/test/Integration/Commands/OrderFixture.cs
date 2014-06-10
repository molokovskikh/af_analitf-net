using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Commands
{
	public class OrderFixture : MixedFixture
	{
		[Test]
		public void Load_orders()
		{
			localSession.DeleteEach<Order>();
			var priceId = localSession.Query<Offer>().First().Price.Id.PriceId;
			Fixture(new UnconfirmedOrder(priceId));

			Run(new UpdateCommand());

			var orders = localSession.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Length);
			var order = orders[0];
			Assert.That(order.Sum, Is.GreaterThan(0));
			Assert.That(order.LinesCount, Is.GreaterThan(0));
			Assert.AreEqual(order.LinesCount, order.Lines.Count);
			Assert.IsFalse(order.Frozen);
			Assert.IsNotNull(order.Address);
			Assert.IsNotNull(order.Price);
		}

		[Test]
		public void Send_orders()
		{
			var order = MakeOrderClean();
			var line = order.Lines[0];

			Run(new SendOrders(address));

			Assert.That(localSession.Query<Order>().Count(), Is.EqualTo(0));
			var sentOrders = localSession.Query<SentOrder>().Where(o => o.SentOn >= begin).ToList();
			Assert.That(sentOrders.Count, Is.EqualTo(1));
			Assert.That(sentOrders[0].Lines.Count, Is.EqualTo(1));

			var orders = session.Query<Common.Models.Order>().Where(o => o.WriteTime >= begin).ToList();
			Assert.That(orders.Count, Is.EqualTo(1));
			var resultOrder = orders[0];
			Assert.That(resultOrder.RowCount, Is.EqualTo(1));
			var item = resultOrder.OrderItems[0];
			Assert.That(item.CodeFirmCr, Is.EqualTo(line.ProducerId));
			Assert.That(item.SynonymCode, Is.EqualTo(line.ProductSynonymId));
			Assert.That(item.SynonymFirmCrCode, Is.EqualTo(line.ProducerSynonymId));

			Assert.That(item.LeaderInfo.MinCost, Is.GreaterThan(0));
			Assert.That(item.LeaderInfo.PriceCode, Is.GreaterThan(0), "номер строки заказа {0}", item.RowId);
			Assert.AreEqual(item.RowId, sentOrders[0].Lines[0].ServerId);
		}

		[Test]
		public void Restore_orders()
		{
			var order = MakeOrderClean();
			order.CreatedOn = order.CreatedOn.AddDays(-1);
			Run(new UpdateCommand());
			var reloaded = localSession.Query<Order>().First();
			Assert.That(reloaded.CreatedOn, Is.EqualTo(order.CreatedOn).Within(1).Seconds);
		}

		[Test]
		public void Reset_order_status()
		{
			localSession.DeleteEach<Order>();

			var prices = localSession.Query<Price>().ToArray();
			var maxId = prices.Max(p => p.Id.PriceId);
			var price = new Price("Тест") {
				RegionName = prices[0].RegionName,
				Id = {
					PriceId = maxId + 10,
					RegionId = prices[0].Id.RegionId
				},
				RegionId = prices[0].Id.RegionId
			};
			localSession.Save(price);
			var order = new Order(price, address);
			var orderLine = new OrderLine {
				Order = order,
				Count = 1,
				OfferId = new OfferComposedId()
			};
			order.AddLine(orderLine);
			localSession.Save(order);

			Run(new UpdateCommand());
			localSession.Refresh(order);
			Assert.IsTrue(order.Frozen);
			Assert.AreEqual(OrderResultStatus.Reject, order.SendResult);

			Run(new UpdateCommand());
			localSession.Refresh(order);
			Assert.IsTrue(order.Frozen);
			Assert.AreEqual(OrderResultStatus.OK, order.SendResult);
		}

		[Test]
		public void Send_result_cost()
		{
			disposable.Add(Disposable.Create(() => Integration.IntegrationSetup.RestoreData(localSession)));
			localSession.DeleteEach<SentOrder>();
			var order = MakeOrderClean();
			order.Price.CostFactor = 1.5m;
			order.Price.VitallyImportantCostFactor = 1.5m;
			Assert.AreEqual(UpdateResult.OK, Run(new SendOrders(address)));

			var sentOrder = localSession.Query<SentOrder>().First(o => o.SentOn >= begin);
			var serverOrder = session.Load<Common.Models.Order>((uint)sentOrder.ServerId);

			var line = order.Lines[0];
			Assert.IsNotNull(line.ResultCost);
			Assert.AreNotEqual(line.ResultCost, line.Cost);
			Assert.AreEqual(line.ResultCost, serverOrder.OrderItems[0].CostWithDelayOfPayment, sentOrder.ServerId.ToString());
		}

		[Test]
		public void Process_batch_request()
		{
			var fixture = new SmartOrder {
				ProductIds = new[] {
					localSession.Query<Offer>().First().ProductId
				}
			};
			Fixture(fixture);
			localSession.DeleteEach<BatchLine>();
			localSession.DeleteEach<Order>();

			MakeBatch("1|5\r\n1-asdasd|10");

			var items = localSession.Query<BatchLine>().ToList();
			Assert.AreEqual(2, items.Count, items.Implode());
			var orders = localSession.Query<Order>().ToList();
			Assert.AreEqual(1, orders.Count, items.Implode());
			Assert.IsFalse(orders[0].Frozen);
			var batchLine = items.First(b => b.Quantity == 5);
			Assert.IsNotNull(batchLine.ExportLineId);
			Assert.IsTrue(batchLine.Status.HasFlag(ItemToOrderStatus.Ordered));
		}

		[Test]
		public void Transit_service_fields()
		{
			var fixture = new SmartOrder {
				ProductIds = new[] {
					localSession.Query<Offer>().First().ProductId
				}
			};
			fixture.Rule.ServiceFields = "2";
			Fixture(fixture);
			localSession.DeleteEach<Order>();

			MakeBatch("1|10|test-payload");
			var items = localSession.Query<BatchLine>().ToList();
			Assert.AreEqual(1, items.Count, items.Implode());
			Assert.That(items[0].ParsedServiceFields, Is.EquivalentTo(new Dictionary<string, string> { { "2", "test-payload" } }));
		}

		[Test]
		public void Freeze_orders()
		{
			var order = MakeOrderClean();

			var fixture = new SmartOrder {
				ProductIds = new[] {
					localSession.Query<Offer>().First().ProductId
				}
			};
			Fixture(fixture);
			MakeBatch("1|10");

			localSession.Refresh(order);
			Assert.IsTrue(order.Frozen);
		}

		[Test]
		public void Transit_error()
		{
			var user = ServerUser();
			user.Client.Settings.SmartOrderRule = null;

			Assert.Throws<EndUserError>(() => MakeBatch("1|10"), "Услуга 'АвтоЗаказ' не предоставляется");
		}

		[Test]
		public void Load_waybill_to_order_mapping()
		{
			MakeOrderClean();

			Run(new SendOrders(address));
			var fixture = Fixture<MatchedWaybill>();
			Run(new UpdateCommand());

			var order = localSession.Query<SentOrder>().First(o => o.SentOn >= begin);
			var docLines = localSession.CreateSQLQuery("select DocumentLineId from WaybillOrders where OrderLineId = :orderLineId")
				.SetParameter("orderLineId", order.Lines[0].ServerId)
				.List();
			Assert.AreEqual(Convert.ToUInt32(docLines[0]), fixture.Waybill.Lines[0].Id);
		}

		private void MakeBatch(string content)
		{
			var filename = TempFile("batch.txt", content);
			Run(new UpdateCommand {
				BatchFile = filename,
				AddressId = localSession.Query<Address>().First().Id
			});
		}
	}
}