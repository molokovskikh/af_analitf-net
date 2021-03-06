﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class OrderLinesFixture : ViewModelFixture<OrderLinesViewModel>
	{
		[SetUp]
		public void Setup()
		{
			session.DeleteEach<Order>();
		}

		[Test]
		public void Delete_order_line()
		{
			var order = MakeOrder();

			Assert.That(model.Lines.Value.Count, Is.EqualTo(1));
			model.CurrentLine.Value = model.Lines.Value.First(l => l.Order.Id == order.Id);
			model.Delete();
			Assert.That(model.Lines.Value.Count, Is.EqualTo(0));
		}

		[Test]
		public void Load_sent_orders()
		{
			model.IsSentSelected.Value = true;
			model.IsCurrentSelected.Value = false;

			Assert.That(model.SentLines, Is.Not.Null);
		}

		[Test]
		public void Filter_by_address()
		{
			restore = true;
			session.DeleteEach(session.Query<Address>().Skip(1));

			var newAddress = new Address("Тестовый адрес доставки");
			session.Save(newAddress);
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);
			MakeOrder(offer, newAddress);

			model.AddressSelector.All.Value = true;
			Assert.That(model.Lines.Value.Count, Is.EqualTo(2));
			model.AddressSelector.Addresses[1].IsSelected = false;
			Assert.That(model.Lines.Value.Count, Is.EqualTo(2));
			scheduler.AdvanceByMs(1000);
			Assert.That(model.Lines.Value.Count, Is.EqualTo(1));
		}

		[Test]
		public void Show_catalog()
		{
			MakeOrder();

			model.CurrentLine.Value = model.Lines.Value.First();
			scheduler.AdvanceByMs(500);
			Assert.That(model.ProductInfo.CanShowCatalog, Is.True);
			model.ProductInfo.ShowCatalog();

			Assert.That(shell.ActiveItem, Is.InstanceOf<CatalogOfferViewModel>());
		}

		[Test]
		public void Print()
		{
			var results = model.PrintPreview().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Delete_line_on_edit()
		{
			var order = MakeOrder();

			model.CurrentLine.Value = model.Lines.Value.FirstOrDefault();
			model.CurrentLine.Value.Count = 0;
			model.Editor.Updated();
			model.Editor.Committed();
			scheduler.AdvanceByMs(5000);
			Assert.That(model.Lines.Value.Count, Is.EqualTo(0));
			Assert.That(model.Sum.Value, Is.EqualTo(0));

			Close(model);

			session.Clear();
			Assert.That(session.Get<Order>(order.Id), Is.Null);
			Assert.That(session.Get<OrderLine>(order.Lines[0].Id), Is.Null);
		}

		[Test]
		public void Update_stat_on_delete()
		{
			MakeOrder();
			model.CurrentLine.Value = model.Lines.Value.FirstOrDefault();

			shell.NotifyOfPropertyChange("CurrentAddress");
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));
			model.CurrentLine.Value.Count = 0;
			model.Editor.Updated();
			scheduler.AdvanceByMs(1000);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(0));
		}

		[Test]
		public void Update_stat()
		{
			MakeOrder();
			model.CurrentLine.Value = model.Lines.Value.FirstOrDefault();
			model.CurrentLine.Value.Count = 100;
			model.Editor.Updated();
			model.Editor.Updated();
			scheduler.AdvanceByMs(1000);
			Assert.That(shell.Stat.Value.Sum, Is.EqualTo(model.CurrentLine.Value.Sum));
		}

		[Test]
		public void Update_sum_on_type_change()
		{
			session.DeleteEach<SentOrder>();
			MakeOrder();

			Assert.That(model.Sum.Value, Is.GreaterThan(0));
			model.IsCurrentSelected.Value = false;
			model.IsSentSelected.Value = true;

			Assert.That(model.Sum.Value, Is.EqualTo(0));

			model.IsSentSelected.Value = false;
			model.IsCurrentSelected.Value = true;
			Assert.That(model.Sum.Value, Is.GreaterThan(0));
		}

		[Test]
		public void Show_catalog_info_on_sent_order_line()
		{
			session.DeleteEach<SentOrder>();
			var sendOrder = MakeSentOrder();
			var catalogId = sendOrder.Lines[0].CatalogId;
			MakeOrder(session.Query<Offer>().First(o => o.CatalogId != catalogId && o.RequestRatio == null));

			model.IsCurrentSelected.Value = false;
			model.IsSentSelected.Value = true;
			scheduler.Start();
			model.SelectedSentLine.Value = model.SentLines.Value.First();
			scheduler.AdvanceByMs(500);
			Assert.AreEqual(catalogId, model.ProductInfo2.CurrentCatalog.Value.Id);
		}

		[Test]
		public void Load_waybill_lines()
		{
			settings.HighlightUnmatchedOrderLines = true;
			session.DeleteEach<SentOrder>();
			var offer = session.Query<Offer>().First();
			var offers = session.Query<Offer>().Where(o => o.Price == offer.Price).Take(2).ToArray();
			var sendOrder = MakeSentOrder(offers);
			CreateMatchedWaybill(sendOrder.Lines[0]);

			model.IsCurrentSelected.Value = false;
			model.IsSentSelected.Value = true;
			scheduler.Start();

			var matchedLine = model.SentLines.Value.First(l => l.Id == sendOrder.Lines[0].Id);
			Assert.IsFalse(matchedLine.IsUnmatchedByWaybill);
			Assert.IsTrue(model.SentLines.Value.First(l => l.Id == sendOrder.Lines[1].Id).IsUnmatchedByWaybill);

			model.SelectedSentLine.Value = matchedLine;
			scheduler.AdvanceByMs(1000);
			Assert.AreEqual(1, model.MatchedWaybills.WaybillLines.Value.Count);
		}

		[Test]
		public void Sync_order_line_with_offers()
		{
			var productId = session.Query<Offer>().GroupBy(o => o.ProductId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.First();
			MakeOrder(session.Query<Offer>().First(o => o.ProductId == productId));
			model.CurrentLine.Value = model.Lines.Value.First();
			scheduler.Start();

			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));
			model.CurrentOffer.Value = model.Offers.Value.First(o => o.OrderCount == null);
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Assert.AreEqual(2, model.Lines.Value.Count);

			model.CurrentOffer.Value.OrderCount = 0;
			model.OfferUpdated();
			model.OfferCommitted();
			Assert.AreEqual(1, model.Lines.Value.Count);
		}

		private void CreateMatchedWaybill(SentOrderLine orderLine)
		{
			var sendOrder = orderLine.Order;
			var waybill = new Waybill {
				ProviderDocumentId = sendOrder.Id.ToString(),
				DocumentDate = DateTime.Now,
				WriteTime = DateTime.Now,
				Address = sendOrder.Address,
				Supplier = session.Load<Supplier>(sendOrder.Price.SupplierId),
			};
			var line = new WaybillLine {
				Product = orderLine.ProductSynonym,
				Producer = orderLine.ProducerSynonym,
				Quantity = (int?)orderLine.Count,
				SupplierCost = orderLine.Cost,
			};
			waybill.AddLine(line);
			waybill.Calculate(settings, new List<uint>());
			session.Save(waybill);
			session.Save(new WaybillOrder(line.Id, orderLine.ServerId.Value));
		}
	}
}