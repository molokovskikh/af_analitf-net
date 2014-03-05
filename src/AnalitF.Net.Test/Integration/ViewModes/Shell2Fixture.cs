using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Linq.Observαble;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using NHibernate.Linq;
using NHibernate.Mapping;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;
using Test.Support.log4net;
using DelayOfPayment = AnalitF.Net.Client.Models.DelayOfPayment;
using Main = AnalitF.Net.Client.ViewModels.Main;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class StubRemoteCommand : RemoteCommand
	{
		public Func<StubRemoteCommand, UpdateResult> Do;
		public UpdateResult result;

		public StubRemoteCommand(UpdateResult result)
		{
			this.result = result;
			Do = c => c.result;
		}

		protected override UpdateResult Execute()
		{
			return Do(this);
		}

		public override UpdateResult Run()
		{
			return Do(this);
		}
	}

	[TestFixture]
	public class Shell2Fixture : ViewModelFixture
	{
		private RemoteCommand command;
		private StubRemoteCommand stub;
		private List<Screen> dialogs;

		[SetUp]
		public void Setup()
		{
			dialogs = new List<Screen>();
			stub = new StubRemoteCommand(UpdateResult.OK);
		}

		protected override ShellViewModel shell
		{
			get
			{
				var value = base.shell;
				value.CommandExecuting += remoteCommand => {
					command = remoteCommand;
					if (stub != null) {
						stub.ErrorMessage = remoteCommand.ErrorMessage;
						stub.SuccessMessage = remoteCommand.SuccessMessage;
					}
					else {
						command.Credentials = null;
					}
					return stub;
				};
				return value;
			}
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
			Assert.IsInstanceOf<PriceOfferViewModel>(shell.ActiveItem,
				prices.CurrentPrice.Value.Id + manager.MessageBoxes.Implode());
			var offers = (PriceOfferViewModel)shell.ActiveItem;
			offers.CurrentOffer.Value.OrderCount = 1;
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
				Deactivate(m);
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
			session.DeleteEach<Order>();
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
			Assert.That(manager.MessageBoxes[0], Is.StringContaining("Обнаружены не отправленные заказы."));
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
			stub.result = UpdateResult.UpdatePending;
			shell.Update();
			var messages = manager.MessageBoxes.Implode();
			Assert.AreEqual(messages, "Получена новая версия программы. Сейчас будет выполнено обновление.");
			Assert.IsFalse(shell.IsActive);
			var cmd = String.Format(@"{0} {1} ""{2}""",
				Path.Combine(config.TmpDir, @"update\update\Updater.exe"),
				Process.GetCurrentProcess().Id,
				typeof(ShellViewModel).Assembly.Location);
			Assert.AreEqual(cmd, ProcessHelper.ExecutedProcesses[0]);
		}

		[Test]
		public void Do_not_warn_on_mandatory_exit()
		{
			stub.result = UpdateResult.UpdatePending;
			MakeOrder();

			shell.Update();
			var messages = manager.MessageBoxes.Implode();
			Assert.AreEqual(messages, "Получена новая версия программы. Сейчас будет выполнено обновление.");
			Assert.IsFalse(shell.IsActive);
			Assert.That(ProcessHelper.ExecutedProcesses[0], Is.StringStarting(Path.Combine(config.TmpDir, @"update\update\Updater.exe")));
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

		[Test]
		public void Execute_command_results()
		{
			stub.Results.Add(new OpenResult("test.txt"));
			shell.Update().Each(r => r.Execute(null));
			var process = ProcessHelper.ExecutedProcesses.Implode();
			Assert.AreEqual("test.txt Open", process);
		}

		[Test]
		public void Reload_settings()
		{
			restore = true;
			var settingsModel = Init<SettingsViewModel>();
			settingsModel.Settings.OpenRejects = !settingsModel.Settings.OpenRejects;
			settingsModel.Save();
			Close(settingsModel);

			testScheduler.AdvanceByMs(1000);
			Assert.AreEqual(settingsModel.Settings.OpenRejects,
				shell.Settings.Value.OpenRejects);
		}

		[Test]
		public void Repeat_request_on_unathorized_exception()
		{
			restore = true;
			var settings = false;
			stub.Do = c => { throw new RequestException("Unauthorized", HttpStatusCode.Unauthorized); };
			manager.ContinueViewDialog = d => {
				if (d is WaitViewModel)
					((WaitViewModel)d).Closed.WaitOne();
				else {
					settings = true;
					stub.Do = c => c.result;
					var model = (SettingsViewModel)d;
					model.Settings.Password = "aioxct2";
					model.Save();
					Close(model);
				}
			};
			shell.Update();

			Assert.That(manager.MessageBoxes.Implode(), Is.StringContaining("Введены некорректные учетные данные"));
			Assert.IsTrue(settings);
			Assert.That(manager.MessageBoxes.Implode(), Is.StringContaining("Обновление завершено успешно"));
			Assert.AreEqual("test", ((NetworkCredential)stub.Credentials).UserName);
			Assert.AreEqual("aioxct2", ((NetworkCredential)stub.Credentials).Password);
		}

		[Test]
		public void Show_correction()
		{
			stub = null;
			var order = Fixture<MakeOrder>().Order;
			Fixture<RandCost>();
			session.Flush();

			Collect(shell.Update());
			Assert.AreEqual(1, dialogs.Count);
			var correction = (Correction)dialogs[0];
			Activate(correction);

			Assert.That(correction.Lines.Count, Is.GreaterThan(0));
			correction.CurrentLine.Value = correction.Lines.First();
			var offers = correction.Offers.Value;
			Assert.IsFalse(correction.IsOrderSend);
			order = session.Query<Order>().First();
			//тк мы оперируем случайными данными то мы можем изменить OfferId заказнной позиции если
			//все остальные атрибуты совпали а цена у нее ниже
			Assert.That(offers.Count, Is.GreaterThan(0));
			var offer = offers.First(o => o.Id == order.Lines[0].OfferId);
			Assert.AreEqual(1, offer.OrderCount,
				String.Format("рассматриваемый offerId = {0}, существующие = {1}",
					order.Lines[0].OfferId,
					session.Query<OrderLine>().Implode(l => String.Format("offerId = {0}, ошибка = {1}", l.OfferId, l.SendError))));
		}

		[Test]
		public void Show_default_item()
		{
			var current = shell.ActiveItem;
			Assert.IsInstanceOf<Main>(current);
			shell.ResetNavigation();
			Assert.AreEqual(current, shell.ActiveItem);
		}

		[Test]
		public void Activate_view()
		{
			shell.ShowCatalog();
			Assert.IsInstanceOf<CatalogViewModel>(shell.ActiveItem);
			shell.ShowPrice();
			Assert.IsInstanceOf<PriceViewModel>(shell.ActiveItem);
		}

		[Test]
		public void Init_newses_url()
		{
			var main = (Main)shell.ActiveItem;
			Assert.IsNotNull(main.Newses.Value[0].Url);
		}

		[Test]
		public void Do_not_warn_on_not_send_orders()
		{
			session.DeleteEach<Order>();
			var offer = session.Query<Offer>().First(o => o.Price.SupplierName.Contains("минимальный заказ"));
			var order1 = MakeOrder(offer);
			order1.Send = false;
			MakeOrder();

			var result = shell.SendOrders().ToArray();
			Assert.AreEqual(0, result.Length, result.Implode());
		}

		[Test]
		public void Recalculate_leaders_on_start()
		{
			restore = true;
			settings.LastUpdate = DateTime.Now;
			settings.LastLeaderCalculation = DateTime.Today.AddDays(-1);
			user.IsDeplayOfPaymentEnabled = true;

			var offer = session.Query<Offer>()
				.First(o => o.LeaderPrice.Id.PriceId != o.Price.Id.PriceId && !o.VitallyImportant);

			var delay = session.Query<DelayOfPayment>().FirstOrDefault(d => d.DayOfWeek == DateTime.Today.DayOfWeek && d.Price == offer.Price)
				?? new DelayOfPayment(-99.999m, offer.Price);
			delay.OtherDelay = -99.999m;
			delay.VitallyImportantDelay = -99.999m;
			session.Save(delay);
			session.Flush();

			manager.DialogOpened.OfType<WaitViewModel>().Subscribe(m => m.Closed.WaitOne());
			shell.OnViewReady().Each(r => r.Execute(new ActionExecutionContext()));
			session.Refresh(offer);
			Assert.AreEqual(offer.Price, offer.LeaderPrice, offer.Id.ToString());
		}

		[Test]
		public void Attach_progress()
		{
			Execute.ResetWithoutDispatcher();
			var ready = new ManualResetEvent(false);
			var done = new ManualResetEvent(false);
			stub.Do = c => {
				ready.WaitOne();
				c.Reporter.Stage("1");
				c.Reporter.Progress();
				c.Reporter.Stage("2");
				done.Set();
				return c.result;
			};
			var events = new List<IObservedChange<SyncViewModel, Progress>>();
			manager.DialogOpened.OfType<SyncViewModel>().Subscribe(m => {
				m.ObservableForProperty(c => c.Progress).Subscribe(events.Add);
				ready.Set();
			});
			shell.Update();

			done.WaitOne(10.Second());
			Assert.AreEqual(4, events.Count, events.Implode(i => i.Value));
		}

		private void Collect(IEnumerable<IResult> results)
		{
			dialogs.AddRange(results.OfType<DialogResult>().Select(d => d.Model));
			foreach (var dialog in dialogs.OfType<BaseScreen>()) {
				dialog.Shell = shell;
			}
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