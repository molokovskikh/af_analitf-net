using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;
using AnalitF.Net.Client.Models.Commands;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class OrdersFixture : ViewModelFixture<OrdersViewModel>
	{
		[Test]
		public void Delete_order()
		{
			var order = PrepareCurrent();

			shell.UpdateStat();
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));
			scheduler.AdvanceByMs(5000);
			Assert.That(model.CanDelete, Is.True);

			// чистим корзину
			//Run(new UpdateCommand());

			//new UpdateCommand().EmptyTrash(0);

			// перемещаем в корзину
			session.DeleteEach<DeletedOrder>();
			session.Flush();
			model.Delete();
			scheduler.AdvanceByMs(5000);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(0));

			model.IsDeletedSelected.Value = true;
			model.IsCurrentSelected.Value = false;
			model.CurrentDeletedOrder = model.DeletedOrders.First();
			model.SelectedDeletedOrders.Add(model.CurrentDeletedOrder);
			Assert.That(model.DeletedOrders.Count(), Is.EqualTo(1));
			Assert.That(model.CurrentDeletedOrder.AddressName.Length, Is.GreaterThan(0));
			Assert.That(model.CurrentDeletedOrder.PriceName.Length, Is.GreaterThan(0));
			Assert.That(model.CurrentDeletedOrder.Price.RegionName.Length, Is.GreaterThan(0));

			// возвращаем из корзины
			TaskResult(model.UnDelete());
			scheduler.AdvanceByMs(5000);
			Assert.That(model.DeletedOrders.Count(), Is.EqualTo(0));
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));

			Close(model);
			session.Clear();
			Assert.Null(session.Get<Order>(order.Id));
		}

		[Test]
		public void Load_order_on_open_tab()
		{
			Assert.That(model.SentOrders, Is.Null);
			model.IsSentSelected.Value = true;
			model.IsCurrentSelected.Value = false;
			scheduler.Start();
			Assert.That(model.SentOrders, Is.Not.Null);
		}

		[Test]
		public void Print()
		{
			MakeOrder(session.Query<Offer>().First());

			model.CurrentOrder = model.Orders.First();
			Assert.That(model.CanPrintStock, Is.True);
			var doc = model.PrintStock().Paginator;
			Assert.That(doc, Is.Not.Null);
		}

		[Test]
		public void Print_sent_order()
		{
			var offer = session.Query<Offer>().First();
			var offers = session.Query<Offer>().Where(o => o.Price == offer.Price).Take(2).ToArray();
			MakeSentOrder(offers);
			model.IsSentSelected.Value = true;
			model.IsCurrentSelected.Value = false;
			Assert.That(model.CanPrintStock, Is.True);
			scheduler.Start();
			var result = model.PrintStock();

			var paginator = result.Paginator;
			Assert.That(paginator, Is.Not.Null);
			var doc = (OrderDocument)result.Docs[0].Value.Item2;
			Assert.AreEqual(2, doc.Lines.Count());
		}

		[Test]
		public void Disable_send_order_button()
		{
			session.DeleteEach<Order>();
			MakeOrder(session.Query<Offer>().First());

			model.Orders[0].Send = false;

			shell.UpdateStat();
			Assert.That(shell.CanSendOrders.Value, Is.True);

			scheduler.AdvanceByMs(5000);
			Assert.That(shell.CanSendOrders.Value, Is.False);
		}

		[Test]
		public void Freeze_order()
		{
			PrepareCurrent();

			shell.UpdateStat();
			Assert.That(shell.CanSendOrders.Value, Is.True);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));

			Assert.True(model.CanFreeze);
			model.Freeze();

			scheduler.AdvanceByMs(1000);
			Assert.That(shell.CanSendOrders.Value, Is.False);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(0));
		}

		[Test]
		public void Unfreeze_order()
		{
			var order = PrepareCurrent();

			Assert.That(model.CanFreeze, Is.True);
			model.Freeze();
			Assert.That(model.CanUnfreeze);
			TaskResult(model.Unfreeze());
			Assert.That(model.Orders.Select(o => o.Id).ToArray(), Is.Not.Contains(order.Id));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
		}

		[Test]
		public void Reorder()
		{
			restore = true;

			session.DeleteEach<Order>();
			var offer = session.Query<Offer>().First(x => !x.Junk);
			MakeOrder(MakeReordarable(offer));
			var order = MakeOrder(offer);

			model.CurrentOrder = model.Orders.First(o => o.Id == order.Id);
			Assert.That(model.CanReorder, Is.True);
			var result = model.Reorder();
			TaskResult(result);

			Assert.That(model.Orders.Select(o => o.Id).ToArray(), Is.Not.Contains(order.Id));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines[0].Count, Is.EqualTo(2));
		}

		[Test]
		public void Reorder_sent_order()
		{
			restore = true;

			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			var offer = session.Query<Offer>().First(x => !x.Junk);
			MakeSentOrder(MakeReordarable(offer));
			MakeOrder(offer);

			SelectSent();

			Assert.That(model.CanReorder, Is.True);
			TaskResult(model.Reorder());
			Assert.That(model.SentOrders.Count, Is.EqualTo(1));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines.Count, Is.EqualTo(2));
		}

		[Test]
		public void Do_not_reorder_on_same_price()
		{
			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			MakeSentOrder();
			MakeOrder();
			SelectSent();

			var result = model.Reorder().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();

			var text = ((TextViewModel)Next<DialogResult>(result).Model).Text;
			Assert.That(text, Does.Contain("предложение отсутствует"));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines.Count, Is.EqualTo(1));
			Assert.That(model.Orders[0].Lines[0].Count, Is.EqualTo(1));
		}

		[Test]
		public void Delete_sent_order()
		{
			var order = PrepareSent();

			Assert.That(model.CanDelete, Is.True);
			model.Delete();
			Assert.That(model.SentOrders, Is.Empty);

			session.Clear();
			Assert.That(session.Get<SentOrder>(order.Id), Is.Null);
		}

		[Test]
		public void Restore_order()
		{
			PrepareSent();

			Assert.That(model.CanRestoreOrder, Is.True);
			TaskResult(model.RestoreOrder());
			Assert.That(model.SentOrders.Count, Is.EqualTo(1));
			model.IsCurrentSelected.Value = true;
			model.IsSentSelected.Value = false;
			Assert.That(model.Orders.Count, Is.EqualTo(1));
		}

		[Test]
		public void Update_personal_comment()
		{
			var order = PrepareSent();

			model.CurrentSentOrder.PersonalComment = "тестовый комментарий";
			session.Refresh(order);
			Assert.AreEqual(order.PersonalComment, "тестовый комментарий");
		}

		[Test]
		public void Select_all_orders()
		{
			session.DeleteEach<Order>();

			restore = true;
			var newAddress = new Address { Name = "Тестовый адрес доставки" };
			session.Save(newAddress);
			MakeOrder();
			shell.CurrentAddress.Value = newAddress;

			Assert.That(model.Orders.Count, Is.EqualTo(0));
			model.AddressSelector.All.Value = true;
			Assert.That(model.Orders.Count, Is.EqualTo(1));
		}

		[Test]
		public void Move()
		{
			restore = true;

			var newAddress = new Address("Я - Тестовый адрес доставки");
			session.Save(newAddress);
			PrepareCurrent();

			model.AddressSelector.All.Value = true;
			model.CurrentOrder = model.Orders.First();
			model.AddressToMove = model.AddressesToMove.Find(a => a.Id == newAddress.Id);
			Assert.True(model.CanMove);
			Assert.That(model.MoveVisible);
			TaskResult(model.Move());
			Assert.That(model.Orders[0].Address.Id, Is.EqualTo(newAddress.Id));
		}

		[Test]
		public void Check_DisplayId_move()
		{
			restore = true;

			var newAddress = new Address("Я - Тестовый адрес доставки");
			session.Save(newAddress);
			PrepareCurrent();

			model.AddressSelector.All.Value = true;
			model.CurrentOrder = model.Orders.First();
			var displayId = model.Orders.First().DisplayId;
			model.AddressToMove = model.AddressesToMove.Find(a => a.Id == newAddress.Id);
			Assert.True(model.CanMove);
			Assert.That(model.MoveVisible);
			TaskResult(model.Move());
			Assert.That(model.Orders[0].Address.Id, Is.EqualTo(newAddress.Id));
			var newDisplayId = displayId + 1;
			Assert.AreEqual(newDisplayId, model.Orders[0].DisplayId);
		}

		[Test(Description = "Тест на список адресов для перемещения заказа на другой адрес")]
		public void Check_Display_AddressesToMove()
		{
			session.DeleteEach<Order>();
			restore = true;

			//Создаем 3 тестовых адреса
			List<Address> testAddressList = new List<Address>();
			for (int i = 1; i <= 3; i++) {
				var testAddress = new Address($"Тестовый адрес доставки{i}");
				testAddressList.Add(testAddress);
				session.Save(testAddress);
			}

			//2 тестовых заказа
			var offer = session.Query<Offer>().First(x => !x.Junk);
			var order1 = MakeOrder(offer, testAddressList[0]);
			var order2 = MakeOrder(offer, testAddressList[1]);

			model.AddressSelector.All.Value = true;

			//Проверяем, текущий заказ можно перенести на другие адреса, в списке которых нет адреса текущего заказа
			model.CurrentOrder = order1;
			//Проверка, что адреса текущего заказа нет в списке для переноса
			Assert.IsFalse(model.AddressesToMove.Any(a => a.Id == model.CurrentOrder.Address.Id));
			//Список адресов, которые должны быть
			var adrListCheck = testAddressList.Where(a => a.Id != model.CurrentOrder.Address.Id).ToList();
			//Проверка на содержание должного списка в писке для переноса
			Assert.IsTrue(adrListCheck.Any(a => model.AddressesToMove.Any(m => m.Id == a.Id)));

			//Тоже самое проверяем для 2го заказа
			model.CurrentOrder = order2;
			Assert.IsFalse(model.AddressesToMove.Any(a => a.Id == model.CurrentOrder.Address.Id));
			adrListCheck = testAddressList.Where(a => a.Id != model.CurrentOrder.Address.Id).ToList();
			Assert.IsTrue(adrListCheck.Any(a => model.AddressesToMove.Any(m => m.Id == a.Id)));

			testAddressList.ForEach(a => session.Delete(a));
		}

		[Test]
		public void Update_stat_after_delete_in_full_view_move()
		{
			PrepareCurrent();

			shell.UpdateStat();
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(1));
			model.AddressSelector.All.Value = true;
			Assert.IsTrue(model.CanDelete);
			model.Delete();

			scheduler.AdvanceByMs(10000);
			Assert.That(shell.Stat.Value.OrdersCount, Is.EqualTo(0));
		}

		[Test]
		public void Enter_sent_order()
		{
			PrepareSent();

			model.EnterSentOrder();

			var currentMode = (OrderDetailsViewModel)shell.ActiveItem;
			Assert.AreEqual(currentMode.Lines.Value.Count, 1);
		}

		[Test]
		public void Update_order()
		{
			var order = PrepareCurrent();
			shell.Navigate(model);

			model.EnterOrder();
			var lines = (OrderDetailsViewModel)shell.ActiveItem;
			lines.CurrentLine.Value = lines.Lines.Value.First();
			lines.Delete();
			Assert.AreEqual(0, lines.Lines.Value.Count);
			Assert.AreEqual(model, shell.ActiveItem);
			Assert.That(model.Orders.Select(o => o.Id), Is.Not.Contains(order.Id));
		}

		[Test(Description = "Тест на ошибку в мапинге хибера, похоже мапинг композитных ключей зависит от порядка полей")]
		public void Min_order_rule()
		{
			var rules = session.Query<MinOrderSumRule>().Select(r => r.Price.Id.PriceId).ToList();
			PrepareCurrent(session.Query<Offer>().First(o => rules.Contains(o.PriceId)));
			Assert.IsNotNull(model.Orders[0].MinOrderSum);
		}

		[Test]
		public void Calculate_order_stat()
		{
			var order = MakeOrder();
			MakeSentOrder(session.Load<Offer>(order.Lines[0].OfferId));

			var modelOrder = model.Orders.First(o => o.Id == order.Id);
			Assert.That(modelOrder.Price.MonthlyOrderSum, Is.GreaterThan(0));
			Assert.That(modelOrder.Price.WeeklyOrderSum, Is.GreaterThan(0));
		}

		[Test]
		public void Try_send_not_active_order_from_orders_view()
		{
			PrepareCurrent();
			model.CurrentOrder.Send = false;
			var e = Assert.Throws<AggregateException>(() => shell.SendOrders().ToArray());
			Assert.AreEqual("Не заказов для отправки", ErrorHelper.TranslateException(e), e.ToString());
		}

		private Offer MakeReordarable(Offer offer)
		{
			var price = session.Query<Price>().First(p => p.Id.PriceId != offer.Price.Id.PriceId);
			var newOffer = new Offer(price, offer, offer.Cost + 50);
			newOffer.Id.OfferId += (ulong)Generator.Random().First();
			session.Save(newOffer);

			return newOffer;
		}

		private Order PrepareCurrent(Offer offer = null)
		{
			session.DeleteEach<Order>();

			var order = MakeOrder(offer);
			model.IsCurrentSelected.Value = true;
			model.CurrentOrder = model.Orders.First();
			model.SelectedOrders.Add(model.CurrentOrder);
			return order;
		}

		private SentOrder PrepareSent()
		{
			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			var order = MakeSentOrder();

			SelectSent();
			return order;
		}

		private void SelectSent()
		{
			model.IsCurrentSelected.Value = false;
			model.IsSentSelected.Value = true;
			scheduler.Start();
			model.CurrentSentOrder = model.SentOrders.First();
			model.SelectedSentOrders.Add(model.CurrentSentOrder);
		}

		[Test]
		public void Load_disabled_order()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();
			model.Update();
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			var priceName = order.Price.Name;
			var regionName = order.Price.RegionName;
			order.Price.Active = false;
			model.Update();
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			Assert.AreNotEqual(model.Orders.First().Price.Name, null);
			Assert.AreEqual(order.Price.Name, priceName);
			Assert.AreEqual(order.Price.RegionName, regionName);
			session.DeleteEach<Order>();
		}

		[Test]
		public void Load_order_without_address()
		{
			session.DeleteEach<Order>();

			restore = true;
			var newAddress = new Address { Name = "Тестовый адрес доставки" };
			session.Save(newAddress);

			var offer = session.Query<Offer>().First(x => !x.Junk);
			MakeOrder(offer, newAddress);

			model.AddressSelector.All.Value = false;
			shell.CurrentAddress.Value = newAddress;
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			session.Delete(newAddress);
			model.AddressSelector.All.Value = false;
			shell.CurrentAddress.Value = null;
			Assert.That(model.Orders.Count, Is.EqualTo(0));
			model.AddressSelector.All.Value = true;
			Assert.That(model.Orders.Count, Is.EqualTo(1));
		}

		[Test]
		public void Restore_order_check_Id()
		{
			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			PrepareCurrent();
			shell.SendOrders().ToArray();
			SelectSent();
			var sentOrderId = model.SentOrders.First().DisplayId;
			model.Update();
			Assert.That(model.CanRestoreOrder, Is.True);
			TaskResult(model.RestoreOrder());
			Assert.That(model.SentOrders.Count, Is.EqualTo(1));
			model.IsCurrentSelected.Value = true;
			model.IsSentSelected.Value = false;
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			var restoreId = model.Orders[0].DisplayId;
			Assert.AreNotEqual(restoreId, sentOrderId);
		}

		[Test]
		public void Unfreeze_order_check_display_id()
		{
			var orderFreeze = PrepareCurrent();
			Assert.That(model.CanFreeze, Is.True);
			model.Freeze();
			Assert.That(model.CanUnfreeze);
			var order = MakeOrder(null);
			var displayId = order.DisplayId;
			TaskResult(model.Unfreeze());
			Assert.That(model.Orders.Select(o => o.Id).ToArray(), Is.Not.Contains(orderFreeze.Id));
			Assert.That(model.Orders.Count, Is.EqualTo(1));
			var displayAfter = model.Orders.First().DisplayId;
			Assert.AreEqual(displayId, displayAfter);
		}

		[Test(Description = "Тест на выделение жирным шрифтом заказы по текущему адресу при действующей галочке 'Все заказы'")]
		public void Check_bold_orders()
		{
			session.DeleteEach<Order>();

			restore = true;
			var newAddress = new Address { Name = "Тестовый адрес доставки" };
			session.Save(newAddress);

			var offer = session.Query<Offer>().First(x => !x.Junk);
			MakeOrder(offer, newAddress);

			model.AddressSelector.All.Value = true;
			shell.CurrentAddress.Value = newAddress;
			Assert.IsTrue(model.Orders.First().IsCurrentAddress);

			shell.CurrentAddress.Value = null;
			Assert.IsFalse(model.Orders.First().IsCurrentAddress);

			session.Delete(newAddress);
		}

	}
}