using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.NHibernate;
using Common.Tools;
using Dapper;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class SyncFixture : MixedFixture
	{
		[Test]
		public void Sync_command()
		{
			settings.LastSync = DateTime.MinValue;
			var stock = new Stock
			{
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				Quantity = 5,
				ReservedQuantity = 0,
				SupplyQuantity = 5
			};

			User User = session?.Query<User>()?.FirstOrDefault()
				?? new User
				{
					SupportHours = "будни: с 07:00 до 19:00",
					SupportPhone = "тел.: 473-260-60-00",
				};

			localSession.Save(stock);

			var doc = new InventoryDoc(address, User);
			doc.Lines.Add(new InventoryLine(doc, stock, 1, localSession));
			doc.UpdateStat();
			doc.Post();
			localSession.Save(doc);
			Run(new SyncCommand());

			TimeMachine.ToFuture(TimeSpan.FromMinutes(10));
			doc.UnPost();
			doc.Post();
			Run(new SyncCommand());
		}

		[Test]
		public void Stock_waybill()
		{
			localSession.Connection.Execute(@"delete from Stocks; delete from StockActions;");
			session.Connection.Execute(@"delete from inventory.StockActions;");
			Run(new SyncCommand());
			var fixture = new CreateWaybill();
			Fixture(fixture);

			Run(new UpdateCommand("Waybills"));
			var waybill = localSession.Load<Waybill>(fixture.Waybill.Log.Id);
			var stockids = waybill.Lines.Where(x => x.StockId != null).Select(x => x.StockId).ToArray();
			var map = localSession.Query<Stock>().Where(x => stockids.Contains(x.ServerId)).ToDictionary(x => x.ServerId);
			waybill.Lines.Each(y =>
			{
				y.Stock = map.GetValueOrDefault(y.StockId);
			});

			waybill.Stock(localSession);
			Assert.AreEqual(DocStatus.Posted, waybill.Status);
			var actions = localSession.Connection.Query<StockAction>("select * from StockActions").ToArray();
			Assert.AreEqual(33, actions.Length);

			var check = new Check(localSession.Query<User>().First(), address, new[] { new CheckLine(waybill.Lines[0].Stock, 1), }, CheckType.SaleBuyer);
			check.Lines.Each(x => x.Doc = check);
			localSession.Save(check);
			localSession.SaveEach(check.Lines);
			localSession.SaveEach(check.Lines.Select(x => x.UpdateStock(x.Stock, CheckType.SaleBuyer)));

			Run(new SyncCommand());
			session.Clear();
			var stocks = session.Query<Service.Models.Inventory.Stock>().Where(x => x.WaybillId == fixture.Waybill.Log.Id).ToList();
			var updatedStock = stocks.First(x => x.Id == waybill.Lines[0].Stock.ServerId);
			actions = localSession.Connection.Query<StockAction>("select * from StockActions").ToArray();
			Assert.AreEqual(34, actions.Length);
			Assert.AreEqual(0, updatedStock.Quantity, $"stock id = {updatedStock.Id}");
			Assert.AreEqual(33, stocks.Count);
			foreach (var stock in stocks)
			{
				Assert.AreEqual(Service.Models.Inventory.StockStatus.Available, stock.Status, $"stock id = {stock.Id}");
			}
			waybill = localSession.Load<Waybill>(fixture.Waybill.Log.Id);
			Assert.AreEqual(DocStatus.Posted, waybill.Status);
			var postedCount = session.Connection
				.Query<int>("select count(*) from Inventory.StockedWaybills where DownloadId = @id", new { id = fixture.Waybill.Log.Id })
				.First();
			Assert.AreEqual(1, postedCount, $"downloadid = {fixture.Waybill.Log.Id}");

			//повторная попытка что бы проверить на ошибку избыточной синхронизации
			Run(new SyncCommand());
			session.Clear();
			var stockCount = session.Query<Service.Models.Inventory.Stock>().Count(x => x.WaybillId == fixture.Waybill.Log.Id);
			updatedStock = stocks.First(x => x.Id == waybill.Lines[0].Stock.ServerId);
			actions = localSession.Connection.Query<StockAction>("select * from StockActions").ToArray();
			Assert.AreEqual(34, actions.Length);
			Assert.AreEqual(0, updatedStock.Quantity, $"stock id = {updatedStock.Id}");
			Assert.AreEqual(33, stockCount);
		}

		[Test]
		public void Exchange()
		{
			localSession.Connection.Execute(@"delete from Stocks; delete from StockActions;");
			session.Connection.Execute(@"delete from inventory.Stocks;");
			session.Connection.Execute(@"delete from inventory.StockActions;");
			session.Connection.Execute(@"delete from  Documents.DocumentHeaders;");
			var stockCount = session.Connection.Query<object>("select * from inventory.Stocks").ToArray();
			var actions = session.Connection.Query<object>("select * from inventory.StockActions").ToArray();
			stockCount = localSession.Connection.Query<object>("select * from Stocks").ToArray();
			actions = localSession.Connection.Query<object>("select * from StockActions").ToArray();

			Run(new SyncCommand());

			var fixture = new CreateWaybill();
			Fixture(fixture);

			Run(new UpdateCommand("Waybills"));
			var waybill = localSession.Load<Waybill>(fixture.Waybill.Log.Id);
			var stockids = waybill.Lines.Where(x => x.StockId != null).Select(x => x.StockId).ToArray();
			var map = localSession.Query<Stock>().Where(x => stockids.Contains(x.ServerId)).ToDictionary(x => x.ServerId);
			waybill.Lines.Each(y =>
			{
				y.Stock = map.GetValueOrDefault(y.StockId);
			});
			waybill.Stock(localSession);
			// чек +0 сток = 33  +1 стокакшин = 34
			var check = new Check(localSession.Query<User>().First(), address, new[] { new CheckLine(waybill.Lines[0].Stock, 1), }, CheckType.SaleBuyer);
			check.Lines.Each(x => x.Doc = check);
			localSession.Save(check);
			localSession.SaveEach(check.Lines);
			localSession.SaveEach(check.Lines.Select(x => x.UpdateStock(x.Stock, CheckType.SaleBuyer)));

			// распаковка +1 сток = 34  +2 стокакшин = 36
			var unpackingDoc = new UnpackingDoc(address, localSession.Query<User>().First());
			var unpackingLine = new UnpackingLine(waybill.Lines[1].Stock, 10);
			unpackingDoc.Lines.Add(unpackingLine);
			unpackingDoc.Post();
			unpackingDoc.PostStockActions();
			localSession.Save(unpackingDoc);
			foreach (var line in unpackingDoc.Lines)
			{
				localSession.Save(line);
			}
			unpackingDoc.PostStockActions();
			foreach (var line in unpackingDoc.Lines)
			{
				localSession.Save(line.SrcStockAction);
				localSession.Save(line.DstStockAction);
			}

			// Списание +0 сток = 34  +1 стокакшин = 37
			var writeoffDoc = new WriteoffDoc(address, localSession.Query<User>().First());
			var writeoffLine = new WriteoffLine(waybill.Lines[2].Stock, 1);
			writeoffDoc.Lines.Add(writeoffLine);
			writeoffDoc.Post(localSession);
			localSession.Save(writeoffDoc);

			//Возврат +0 сток = 34  +1 стокакшин = 38
			var ReturnDoc = new ReturnDoc(address, localSession.Query<User>().First());
			ReturnDoc.Supplier = waybill.Supplier;
			var ReturnLine = new ReturnLine(waybill.Lines[3].Stock, 1);
			ReturnDoc.Lines.Add(ReturnLine);
			ReturnDoc.Post(localSession);
			localSession.Save(ReturnDoc);

			//переоценка +1 сток = 35  +2 стокакшин = 40
			var ReassessmentDoc = new ReassessmentDoc(address, localSession.Query<User>().First());
			var stock = waybill.Lines[4].Stock.Copy();
			stock.RetailCost += 10;
			var ReassessmentLine = new ReassessmentLine(waybill.Lines[4].Stock, stock);
			ReassessmentDoc.Lines.Add(ReassessmentLine);
			ReassessmentDoc.Post(localSession);
			localSession.Save(ReassessmentDoc);

			//переоценка +0 сток = 35  +1 стокакшин = 41
			var InventoryDoc = new InventoryDoc(address, localSession.Query<User>().First());
			var InventoryLine = new InventoryLine(InventoryDoc, waybill.Lines[5].Stock, 5, localSession);
			InventoryDoc.Lines.Add(InventoryLine);
			InventoryDoc.Post();
			localSession.Save(InventoryDoc);

			//Перемещение +1 сток = 36  +2 стокакшин = 43
			var DisplacementDoc = new DisplacementDoc(address, localSession.Query<User>().First());
			var DisplacementLine = new DisplacementLine(waybill.Lines[6].Stock, waybill.Lines[6].Stock.Copy(), 5);
			DisplacementDoc.Lines.Add(DisplacementLine);
			DisplacementDoc.Post(localSession);
			localSession.Save(DisplacementDoc);

			Run(new SyncCommand());
			stockCount = session.Connection.Query<object>("select * from inventory.Stocks").ToArray();
			actions = session.Connection.Query<object>("select * from inventory.StockActions").ToArray();
			Assert.AreEqual(36, stockCount.Length);
			Assert.AreEqual(43, actions.Length);

			Run(new SyncCommand());
			stockCount = session.Connection.Query<object>("select * from inventory.Stocks").ToArray();
			actions = session.Connection.Query<object>("select * from inventory.StockActions").ToArray();
			Assert.AreEqual(36, stockCount.Length);
			Assert.AreEqual(43, actions.Length);
		}
	}
}