﻿using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.MySql;
using Common.Tools;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class StubRemoteCommand : RemoteCommand
	{
		private UpdateResult result;

		public StubRemoteCommand(UpdateResult result)
		{
			this.result = result;
		}

		protected override UpdateResult Execute()
		{
			throw new NotImplementedException();
		}

		public override UpdateResult Run()
		{
			return result;
		}
	}

	[TestFixture]
	public class Shell2Fixture : BaseFixture
	{
		private RemoteCommand command;
		private UpdateResult result;

		[SetUp]
		public void Setup()
		{
			result = UpdateResult.OK;
			shell.CommandExecuting += remoteCommand => {
				command = remoteCommand;
				var stub = new StubRemoteCommand(result);
				stub.ErrorMessage = remoteCommand.ErrorMessage;
				stub.SuccessMessage = remoteCommand.SuccessMessage;
				return stub;
			};
			Tasks.ExtractPath = Path.Combine("temp", "update");
		}

		[Test]
		public void Update_order_stat_on_order_change()
		{
			session.DeleteEach<Order>();
			session.Flush();

			shell.ShowPrice();
			var prices = (PriceViewModel)shell.ActiveItem;
			prices.CurrentPrice.Value = prices.Prices.First(p => p.PositionCount > 0);
			prices.EnterPrice();
			var offers = (PriceOfferViewModel)shell.ActiveItem;
			offers.CurrentOffer.OrderCount = 1;
			offers.OfferUpdated();
			offers.OfferCommitted();

			testScheduler.AdvanceByMs(1000);

			offers.NavigateBackward();

			testScheduler.AdvanceByMs(1000);

			var stat = shell.Stat.Value;
			Assert.That(stat.OrdersCount, Is.EqualTo(1));
			Assert.That(stat.OrderLinesCount, Is.EqualTo(1));
			Assert.That(stat.Sum, Is.GreaterThan(0));
		}

		[Test]
		public void Run_update_after_configure()
		{
			restore = true;

			settings.LastUpdate = null;
			settings.UserName = null;
			settings.Password = null;
			session.Flush();
			shell.Reload();

			ContinueWithDialog<SettingsViewModel>(m => {
				m.Settings.UserName = "test";
				m.Settings.Password = "123";
				m.Save();
			});

			shell.StartCheck();

			Assert.That(shell.Settings.Value.UserName, Is.Not.Null);
			Assert.That(shell.Settings.Value.Password, Is.Not.Null);
			Assert.That(manager.MessageBoxes[0], Is.StringContaining("необходимо заполнить учетные данные"));
			Assert.That(manager.MessageBoxes[1], Is.StringContaining("База данных программы не заполнена"));
			Assert.That(manager.MessageBoxes[2], Is.StringContaining("Обновление завершено успешно"));
			Assert.That(command, Is.InstanceOf<UpdateCommand>());
		}

		[Test]
		public void Check_last_update_time()
		{
			restore = true;

			settings.LastUpdate = DateTime.Now.AddDays(-1);
			settings.UserName = "test";
			settings.Password = "123";
			session.Flush();
			shell.Reload();

			shell.StartCheck();
			Assert.That(manager.MessageBoxes[0], Is.StringContaining("Вы работаете с устаревшим набором данных."));
			Assert.That(manager.MessageBoxes[1], Is.StringContaining("Обновление завершено успешно"));
			Assert.That(command, Is.InstanceOf<UpdateCommand>());
		}

		[Test]
		public void Check_orders_on_close()
		{
			MakeOrder();

			settings.LastUpdate = DateTime.Now.AddDays(-1);
			settings.UserName = "test";
			settings.Password = "123";
			session.Flush();
			shell.Reload();

			shell.NotifyOfPropertyChange("CurrentAddress");
			var canClose = false;
			shell.CanClose(b => canClose = b);

			Assert.IsTrue(canClose);
			Assert.That(manager.MessageBoxes[0], Is.StringContaining("Обнаружены неотправленные заказы."));
			Assert.That(manager.MessageBoxes[1], Is.StringContaining("Отправка заказов завершена успешно."));
			Assert.That(command, Is.InstanceOf<SendOrders>());
		}

		[Test]
		public void Show_about()
		{
			shell.ShowAbout();
		}

		[Test]
		public void On_pending_update_close_shell_and_execute_updater()
		{
			result = UpdateResult.UpdatePending;
			shell.Update();
			var messages = manager.MessageBoxes.Implode();
			Assert.AreEqual(messages, "Получена новая версия программы. Сейчас будет выполнено обновление.");
			Assert.IsFalse(shell.IsActive);
			Assert.That(shell.StartedProcess[0], Is.StringStarting(@"temp\update\update\Updater.exe"));
		}

		[Test]
		public void Do_not_warn_on_mandatory_exit()
		{
			result = UpdateResult.UpdatePending;
			MakeOrder();

			shell.Update();
			var messages = manager.MessageBoxes.Implode();
			Assert.AreEqual(messages, "Получена новая версия программы. Сейчас будет выполнено обновление.");
			Assert.IsFalse(shell.IsActive);
			Assert.That(shell.StartedProcess[0], Is.StringStarting(@"temp\update\update\Updater.exe"));
		}

		[Test]
		public void Delete_old_orders()
		{
			session.DeleteEach(session.Query<Order>());
			var order = MakeSentOrder();
			order.SentOn = order.SentOn.AddMonths(-2);
			session.Save(order);
			session.Flush();

			var canClose = false;
			shell.UpdateStat();
			shell.CanClose(b => canClose = b);

			session.Evict(order);
			var reloaded = session.Get<SentOrder>(order.Id);
			Assert.IsTrue(canClose);
			Assert.IsNull(reloaded);
			Assert.AreEqual("В архиве заказов обнаружены заказы, сделанные более 35 дней назад. Удалить их?",
				manager.MessageBoxes.Implode());
		}

		private void ContinueWithDialog<T>(Action<T> action)
		{
			manager.ContinueViewDialog = m => {
				if (m is T) {
					ScreenExtensions.TryActivate(m);
					action((T)m);
					Close(m);
				}
			};
		}
	}
}