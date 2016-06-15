using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Service.Models;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Address = AnalitF.Net.Client.Models.Address;
using OfferComposedId = AnalitF.Net.Client.Models.OfferComposedId;
using OrderResultStatus = AnalitF.Net.Client.Models.OrderResultStatus;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class OrderFixture : MixedFixture
	{
		[Test]
		public void Reject_order_by_min_req()
		{
			var offer = localSession.Query<Offer>().First(o => o.Price.SupplierName.Contains("минимальный заказ"));
			var order = MakeOrderClean(address, offer);

			var command = new SendOrders(address);
			Run(command);

			var results = command.Results.ToArray();
			var text = results
				.OfType<DialogResult>()
				.Select(d => d.Model)
				.OfType<TextViewModel>()
				.Select(t => t.Text)
				.FirstOrDefault();
			var expected = $"прайс-лист {order.Price.Name} - Поставщик отказал в приеме заказа." +
				" Сумма заказа меньше минимально допустимой." + $" Минимальный заказ {1500:0.00} заказано {order.Sum:0.00}.";
			Assert.AreEqual(expected, text, results.Implode());
		}

		[Test]
		public void Load_orders()
		{
			localSession.DeleteEach<Order>();
			var order = MakeOrder();
			var fixture = new UnconfirmedOrder(order.Price.Id.PriceId);
			Fixture(fixture);

			Run(new UpdateCommand());

			var orders = localSession.Query<Order>().ToArray();
			Assert.AreEqual(2, orders.Length);

			var loaded = orders.First(x => x.Id != order.Id);
			Assert.That(loaded.Sum, Is.GreaterThan(0));
			Assert.That(loaded.LinesCount, Is.GreaterThan(0));
			Assert.AreEqual(loaded.LinesCount, loaded.Lines.Count);
			Assert.IsFalse(loaded.Frozen, "Заказ заморожен {0}", loaded.SendError + loaded.Lines.Implode(l => l.SendError));
			Assert.IsNotNull(loaded.Address);
			Assert.IsNotNull(loaded.Price);

			localSession.Refresh(order);
			Assert.IsTrue(order.Frozen);
			Assert.AreEqual(1, order.Lines.Count);

			var log = session.Query<RequestLog>().OrderByDescending(x => x.Id).First();
			//протоколируем номер заказа на клиенте и сервере
			Assert.AreEqual($"Экспортированы неподтвержденные заявки: {fixture.Order.Id} -> {order.Id + 1}", log.Error);
		}

		[Test]
		public void Send_orders()
		{
			localSession.DeleteEach<SentOrder>();
			var order = MakeOrderClean();
			var line = order.Lines[0];

			Run(new SendOrders(address));

			Assert.That(localSession.Query<Order>().Count(), Is.EqualTo(0));
			var srcOrders = localSession.Query<SentOrder>().Where(o => o.SentOn >= begin).ToList();
			var srcOrder = srcOrders[0];
			Assert.That(srcOrders.Count, Is.EqualTo(1), srcOrders.Implode());
			Assert.That(srcOrder.Lines.Count, Is.EqualTo(1));

			var dstOrders = session.Query<Common.Models.Order>().Where(o => o.WriteTime >= begin).ToList();
			var dstOrder = dstOrders[0];
			Assert.AreEqual(dstOrder.RowId, srcOrder.ServerId);
			Assert.AreEqual(1, dstOrders.Count);
			Assert.IsFalse(dstOrders[0].Deleted);
			Assert.AreEqual(1, dstOrder.RowCount);
			Assert.AreEqual("Базовый", dstOrder.PriceName);
			Assert.AreEqual(session.Load<TestPrice>(dstOrder.PriceList.PriceCode).Costs[0].Id, dstOrder.CostId);
			Assert.AreEqual("Тестовая", dstOrder.CostName);

			var item = dstOrder.OrderItems[0];
			Assert.That(item.CodeFirmCr, Is.EqualTo(line.ProducerId));
			Assert.That(item.SynonymCode, Is.EqualTo(line.ProductSynonymId));
			Assert.That(item.SynonymFirmCrCode, Is.EqualTo(line.ProducerSynonymId));

			Assert.That(item.LeaderInfo.MinCost, Is.GreaterThan(0));
			Assert.That(item.LeaderInfo.PriceCode, Is.GreaterThan(0), "номер строки заказа {0}", item.RowId);
			Assert.AreEqual(item.RowId, srcOrder.Lines[0].ServerId);
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
			disposable.Add(Disposable.Create(() => DbHelper.RestoreData(localSession)));
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
					SafeSmartOrderProductId()
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
			Assert.IsNotNull(batchLine.ExportId);
			Assert.IsTrue(batchLine.Status.HasFlag(ItemToOrderStatus.Ordered));
			var orderline = orders.SelectMany(o => o.Lines).FirstOrDefault(l => l.ExportBatchLineId == batchLine.ExportId);
			Assert.IsNotNull(orderline);
		}

		[Test]
		public void Reprocess_batch()
		{
			var fixture = new SmartOrder {
				ProductIds = new[] {
					SafeSmartOrderProductId()
				}
			};
			Fixture(fixture);
			localSession.DeleteEach<BatchLine>();
			localSession.DeleteEach<Order>();

			MakeBatch("1|5");

			var items = localSession.Query<BatchLine>().ToList();
			var batchLine = items[0];
			Assert.IsTrue(batchLine.Status.HasFlag(ItemToOrderStatus.Ordered));

			batchLine.Quantity = 3;
			Run(new UpdateCommand {
				SyncData = "Batch",
				AddressId = localSession.Query<Address>().First().Id
			});
			items = localSession.Query<BatchLine>().ToList();
			batchLine = items[0];
			Assert.IsTrue(batchLine.Status.HasFlag(ItemToOrderStatus.Ordered), batchLine.ToString());
			Assert.AreEqual(3, batchLine.Quantity);
			var lines = localSession.Query<OrderLine>().ToArray();
			var orderLine = lines[0];
			Assert.AreEqual(1, lines.Length);
			Assert.AreEqual(3, orderLine.Count);
		}

		[Test]
		public void Ignore_restore_order_for_batch()
		{
			localSession.DeleteEach<Order>();

			var price = session.Load<TestPrice>(localSession.Query<Price>().First().Id.PriceId);
			var ids = localSession.Query<Offer>().Select(x => x.ProductId).Distinct().ToArray();
			var product = session.Query<TestProduct>().First(x => !x.Hidden && !ids.Contains(x.Id));
			var offer = price.Supplier.AddCore(product);
			price.Supplier.SaveCore(session, offer);

			offer.RequestRatio = 5;
			price.Supplier.InvalidateCache(session, ServerUser().Id);

			var fixture = new SmartOrder();
			fixture.Rule.CheckOrderCost = false;
			fixture.Rule.CheckMinOrderCount = false;
			fixture.Rule.CheckRequestRatio = false;
			fixture.ProductIds = new [] { offer.Product.Id };
			Fixture(fixture);

			MakeBatch("1|1");
			var orders = localSession.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Length);
			var order = orders[0];
			Assert.IsFalse(order.Frozen, order.Lines.Implode(x => x.SendError));
			Assert.AreEqual(offer.Id, order.Lines[0].OfferId.OfferId);
		}

		[Test]
		public void Transit_service_fields()
		{
			var fixture = new SmartOrder {
				ProductIds = new[] {
					SafeSmartOrderProductId()
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
					SafeSmartOrderProductId()
				}
			};
			Fixture(fixture);
			MakeBatch("1|10");

			//здесь может возникнуть NHibernate.UnresolvableObjectException : No row with the given identifier exists
			//это значить что данные в backup не соответсвуют данным в data нужно перезалить данные
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

			Assert.AreEqual(UpdateResult.OK, Run(new SendOrders(address)));
			var fixture = Fixture<MatchedWaybill>();
			Assert.AreEqual(UpdateResult.SilentOk, Run(new UpdateCommand()));

			var order = localSession.Query<SentOrder>().First(o => o.SentOn >= begin);
			var docLines = localSession
				.CreateSQLQuery("select DocumentLineId from WaybillOrders where OrderLineId = :orderLineId")
				.SetParameter("orderLineId", order.Lines[0].ServerId)
				.List();
			Assert.AreEqual(1, docLines.Count, $"не удалось найти накладную для заявки {order.Id}");
			Assert.AreEqual(Convert.ToUInt32(docLines[0]), fixture.Waybill.Lines[0].Id);
		}

		[Test]
		public void Save_orders()
		{
			var ordersPath = settings.MapPath("Orders");
			if (Directory.Exists(ordersPath))
				Directory.GetFiles(ordersPath).Each(File.Delete);

			localSession.DeleteEach<Order>();
			localSession.DeleteEach<SentOrder>();
			var offer = SafeSmartOrderOffer();

			var externalLineId = Guid.NewGuid().ToString();
			var externalProductId = Guid.NewGuid().ToString();
			var externalAddressId = Guid.NewGuid().ToString();
			var fixture = new SmartOrder {
				SynonymMap = {
					Tuple.Create(offer.ProductSynonym, offer.ProductId)
				},
				AddressMap = {
					Tuple.Create(externalAddressId, address.Id)
				}
			};
			fixture.Rule.ParseAlgorithm = "HealthyPeopleSource";
			session.CreateSQLQuery("update usersettings.RetClientsSet set SaveOrders = 1 where ClientCode = :clientId")
				.SetParameter("clientId", ServerUser().Client.Id)
				.ExecuteUpdate();

			session.Transaction.Commit();
			Fixture(fixture);

			var batch = $@"Номер;Аптека;Дата;Код;Товар;ЗаводШК;Производитель;Количество;Приоритет;Цена
{externalLineId};{externalAddressId};{DateTime.Now};{externalProductId};{offer.ProductSynonym};;;1;;";

			Assert.AreEqual(UpdateResult.OK, MakeBatch(batch));
			Assert.AreEqual(1, localSession.Query<Order>().Count(), localSession.Query<BatchLine>().Implode());

			Assert.AreEqual(UpdateResult.OK, Run(new SendOrders(address)));
			var files = Directory.GetFiles(ordersPath);
			Assert.AreEqual(1, files.Length);
			var order = localSession.Query<SentOrder>().First();
			var expected = $@"Номер;Аптека;Дата;Код;Товар;ЗаводШК;Производитель;Количество;Приоритет;Цена;Поставщик
{externalLineId};{externalAddressId};{order.SentOn};{externalProductId};{order.Lines[0].ProductSynonym};;{order.Lines[0].ProducerSynonym};1;;;{order.Price.Name}";
			var lines = File.ReadAllText(files[0], Encoding.Default).TrimEnd();
			Assert.AreEqual(expected, lines);
		}

		[Test]
		public void Save_unordered()
		{
			localSession.DeleteEach<BatchLine>();
			localSession.DeleteEach<Order>();

			Fixture(new SmartOrder {
				ProductIds = new[] {
					SafeSmartOrderProductId()
				}
			});
			//код 1 есть будет заказана
			//кода 2 нет по этому позиция будет не заказана
			MakeBatch("1|1\r\n2|5");

			var lines = localSession.Query<BatchLine>().ToList();
			Assert.AreEqual(2, lines.Count);
			Assert.IsTrue(lines.First(x => x.Code == "2").IsNotOrdered);

			//должны удалить заказанную позицию
			Run(new UpdateCommand {
				SyncData = "Batch",
				AddressId = localSession.Query<Address>().First().Id,
				BatchMode = BatchMode.SaveUnordered,
				BatchFile = TempFile("batch.txt", "2|5")
			});
			lines = localSession.Query<BatchLine>().ToList();
			Assert.AreEqual(2, lines.Count);
			Assert.IsTrue(lines[0].IsNotOrdered);
			Assert.AreEqual("2", lines[0].Code);
			Assert.IsTrue(lines[1].IsNotOrdered);
			Assert.AreEqual("2", lines[1].Code);
		}

		//для автозаказа нам нужна такая позиция которой нет в прайсе с минимальной ценой заказа
		//если позиция с минимальной суммой окажется самой дешевой то автозаказ не сможет сформировать заявку
		public uint SafeSmartOrderProductId()
		{
			return SafeSmartOrderOffer().ProductId;
		}

		private Offer SafeSmartOrderOffer()
		{
			var productIds = localSession.Query<Offer>()
				.Where(o => o.Price.Name.Contains("минимальный заказ"))
				.Select(o => o.ProductId)
				.Distinct()
				.ToArray();
			return localSession.Query<Offer>().First(o => !productIds.Contains(o.ProductId) && o.RequestRatio == null && !o.Junk);
		}

		private UpdateResult MakeBatch(string content)
		{
			return Run(new UpdateCommand {
				SyncData = "Batch",
				BatchFile = TempFile("batch.txt", content),
				AddressId = localSession.Query<Address>().First().Id
			});
		}
		[Test]
		public void Check_DislayId()
		{
			localSession.DeleteEach<Order>();
			var order = MakeOrder();
			var id = order.DisplayId;
			Run(new UpdateCommand());
			var orders = localSession.Query<Order>().ToArray();
			Assert.AreEqual(2, orders.Length);
			var loaded = orders.First(x => x.Id != order.Id);
		}
	}
}