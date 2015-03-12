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
using Common.NHibernate;
using Common.Tools;
using log4net.Config;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using Test.Support.log4net;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Test.Integration.Commands
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

			var text = command.Results
				.OfType<DialogResult>()
				.Select(d => d.Model)
				.OfType<TextViewModel>()
				.Select(t => t.Text)
				.FirstOrDefault();
			var expected = String.Format("прайс-лист {0} - Поставщик отказал в приеме заказа." +
				" Сумма заказа меньше минимально допустимой." +
				" Минимальный заказ {1:0.00} заказано {2:0.00}.", order.Price.Name, 1500, order.Sum);
			Assert.AreEqual(expected, text);
		}

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
			Assert.IsFalse(orders[0].Deleted);
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
			disposable.Add(Disposable.Create(() => DataHelper.RestoreData(localSession)));
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

			Run(new SendOrders(address));
			var fixture = Fixture<MatchedWaybill>();
			Run(new UpdateCommand());

			var order = localSession.Query<SentOrder>().First(o => o.SentOn >= begin);
			var docLines = localSession.CreateSQLQuery("select DocumentLineId from WaybillOrders where OrderLineId = :orderLineId")
				.SetParameter("orderLineId", order.Lines[0].ServerId)
				.List();
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

			var batch = String.Format(@"Номер;Аптека;Дата;Код;Товар;ЗаводШК;Производитель;Количество;Приоритет;Цена
{0};{1};{2};{3};{4};;;1;;", externalLineId, externalAddressId, DateTime.Now, externalProductId, offer.ProductSynonym);

			Assert.AreEqual(UpdateResult.OK, MakeBatch(batch));
			Assert.AreEqual(1, localSession.Query<Order>().Count(), localSession.Query<BatchLine>().Implode());

			Assert.AreEqual(UpdateResult.OK, Run(new SendOrders(address)));
			var files = Directory.GetFiles(ordersPath);
			Assert.AreEqual(1, files.Length);
			var order = localSession.Query<SentOrder>().First();
			var expected = String.Format(@"Номер;Аптека;Дата;Код;Товар;ЗаводШК;Производитель;Количество;Приоритет;Цена
{0};{1};{2};{3};{4};;;1;;", externalLineId, externalAddressId, order.SentOn, externalProductId, offer.ProductSynonym);
			var lines = File.ReadAllText(files[0], Encoding.Default).TrimEnd();
			Assert.AreEqual(expected, lines);
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
			return localSession.Query<Offer>().First(o => !productIds.Contains(o.ProductId));
		}

		private UpdateResult MakeBatch(string content)
		{
			var filename = TempFile("batch.txt", content);
			return Run(new UpdateCommand {
				SyncData = "Batch",
				BatchFile = filename,
				AddressId = localSession.Query<Address>().First().Id
			});
		}
	}
}