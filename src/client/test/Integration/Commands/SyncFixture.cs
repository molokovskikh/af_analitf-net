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
			var stock = new Stock {
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				Quantity = 5,
				ReservedQuantity = 0,
				SupplyQuantity = 5
			};
			localSession.Save(stock);

			var doc = new InventoryDoc(address);
			doc.Lines.Add(new InventoryLine(stock, 1, localSession));
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
			var fixture = new CreateWaybill();
			Fixture(fixture);

			Run(new UpdateCommand("Waybills"));
			var waybill = localSession.Load<Waybill>(fixture.Waybill.Log.Id);
			var stockids = waybill.Lines.Where(x => x.StockId != null).Select(x => x.StockId).ToArray();
			var map = localSession.Query<Stock>().Where(x => stockids.Contains(x.ServerId)).ToDictionary(x => x.ServerId);
			waybill.Lines.Each(y => {
				y.Stock = map.GetValueOrDefault(y.StockId);
			});

			waybill.Stock(localSession);
			Assert.AreEqual(DocStatus.Posted, waybill.Status);

			var check = new Check(localSession.Query<User>().First(), address, new [] { new CheckLine(waybill.Lines[0].Stock, 1), }, CheckType.SaleBuyer);
			localSession.Save(check);
			localSession.SaveEach(check.Lines);
			localSession.SaveEach(check.Lines.Select(x => x.UpdateStock(x.Stock)));

			Run(new SyncCommand());
			session.Clear();
			var stocks = session.Query<Service.Models.Inventory.Stock>().Where(x => x.WaybillId == fixture.Waybill.Log.Id).ToList();
			var updatedStock = stocks.First(x => x.Id == waybill.Lines[0].Stock.ServerId);
			Assert.AreEqual(0, updatedStock.Quantity, $"stock id = {updatedStock.Id}");
			Assert.AreEqual(33, stocks.Count);
			foreach (var stock in stocks) {
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
			Assert.AreEqual(0, updatedStock.Quantity, $"stock id = {updatedStock.Id}");
			Assert.AreEqual(33, stockCount);
		}
	}
}