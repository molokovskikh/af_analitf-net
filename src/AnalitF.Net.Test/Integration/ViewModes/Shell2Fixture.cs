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
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class Shell2Fixture : BaseFixture
	{
		private string command;

		[SetUp]
		public void Setup()
		{
			command = "";
			Tasks.Update = (credentials, token, arg3) => {
				command = "Update";
				return UpdateResult.OK;
			};

			Tasks.SendOrders = (credentials, token, arg3, arg4) => {
				command = "SendOrders";
				return UpdateResult.OK;
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
			Assert.That(command, Is.EqualTo("Update"));
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
			Assert.That(command, Is.EqualTo("Update"));
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
			Assert.That(command, Is.EqualTo("SendOrders"));
		}

		[Test]
		public void Show_about()
		{
			shell.ShowAbout();
		}

		[Test]
		public void On_pending_update_close_shell_and_execute_updater()
		{
			Tasks.Update = (credentials, token, arg3) => {
				return UpdateResult.UpdatePending;
			};

			shell.Update();
			var messages = manager.MessageBoxes.Implode();
			Assert.AreEqual(messages, "Получена новая версия программы. Сейчас будет выполнено обновление.");
			Assert.IsFalse(shell.IsActive);
			Assert.That(shell.StartedProcess[0], Is.StringStarting(@"temp\update\update\Updater.exe"));
		}

		[Test]
		public void Do_not_warn_on_mandatory_exit()
		{
			Tasks.Update = (credentials, token, arg3) => {
				return UpdateResult.UpdatePending;
			};
			MakeOrder();

			shell.Update();
			var messages = manager.MessageBoxes.Implode();
			Assert.AreEqual(messages, "Получена новая версия программы. Сейчас будет выполнено обновление.");
			Assert.IsFalse(shell.IsActive);
			Assert.That(shell.StartedProcess[0], Is.StringStarting(@"temp\update\update\Updater.exe"));
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