using System;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
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
		private bool executed;

		[SetUp]
		public void Setup()
		{
			executed = false;
			Tasks.Update = (credentials, token, arg3) => {
				executed = true;
				return UpdateResult.OK;
			};
		}

		[Test]
		public void Update_order_stat_on_order_change()
		{
			session.DeleteEach<Order>();
			session.Flush();

			shell.ShowPrice();
			var prices = (PriceViewModel)shell.ActiveItem;
			prices.CurrentPrice = prices.Prices.First(p => p.PositionCount > 0);
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
			Restore = true;

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
			Assert.That(executed, Is.True);
		}

		[Test]
		public void Check_last_update_time()
		{
			Restore = true;

			settings.LastUpdate = DateTime.Now.AddDays(-1);
			settings.UserName = "test";
			settings.Password = "123";
			session.Flush();
			shell.Reload();

			shell.StartCheck();
			Assert.That(manager.MessageBoxes[0], Is.StringContaining("Вы работаете с устаревшим набором данных."));
			Assert.That(manager.MessageBoxes[1], Is.StringContaining("Обновление завершено успешно"));
			Assert.That(executed, Is.True);
		}

		private void ContinueWithDialog<T>(Action<T> action)
		{
			manager.ContinueViewDialog = m => {
				if (m is T) {
					ScreenExtensions.TryActivate(m);
					action((T)m);
					ScreenExtensions.TryDeactivate(m, true);
				}
			};
		}
	}
}