using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Tools.Threading;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using DelayOfPayment = AnalitF.Net.Client.Models.DelayOfPayment;
using Main = AnalitF.Net.Client.ViewModels.Main;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
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
	public class ShellFixture : ViewModelFixture
	{
		private RemoteCommand command;
		private StubRemoteCommand stub;
		private List<Screen> dialogs;

		[SetUp]
		public void Setup()
		{
			command = null;
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
			prices.CurrentPrice.Value = prices.Prices.Value.First(p => p.PositionCount > 0);
			prices.EnterPrice();
			Assert.IsInstanceOf<PriceOfferViewModel>(shell.ActiveItem,
				prices.CurrentPrice.Value.Id + manager.MessageBoxes.Implode());
			var offers = (PriceOfferViewModel)shell.ActiveItem;
			scheduler.Start();
			offers.CurrentOffer.Value.OrderCount = 1;
			offers.OfferUpdated();
			offers.OfferCommitted();

			scheduler.AdvanceByMs(1000);

			offers.NavigateBackward();

			scheduler.AdvanceByMs(1000);

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

			manager.DialogOpened.OfType<SettingsViewModel>().Subscribe(m => {
				ScreenExtensions.TryActivate(m);
				m.Settings.Value.UserName = "test";
				m.Settings.Value.Password = "123";
				var results = m.Save().ToList();
				Deactivate(m);
				Close(m);
			});

			shell.StartCheck();

			Assert.That(shell.Settings.Value.UserName, Is.Not.Null);
			Assert.That(shell.Settings.Value.Password, Is.Not.Null);
			Assert.That(manager.MessageBoxes[0], Does.Contain("необходимо заполнить учетные данные"));
			Assert.That(manager.MessageBoxes[1], Does.Contain("База данных программы не заполнена"));
			Assert.That(manager.MessageBoxes[2], Does.Contain("Обновление завершено успешно"));
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
			Assert.That(manager.MessageBoxes[0], Does.Contain("Вы работаете с устаревшим набором данных."));
			Assert.That(manager.MessageBoxes[1], Does.Contain("Обновление завершено успешно"));
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
			scheduler.Start();

			var canClose = new Subject<bool>();
			var canCloseValue = canClose.Take(1).PublishLast();
			canCloseValue.Connect();
			shell.CanClose(b => canClose.OnNext(b));
			Assert.IsTrue(canCloseValue.Timeout(TimeSpan.FromSeconds(3)).First());

			Assert.That(manager.MessageBoxes[0], Does.Contain("Обнаружены не отправленные заказы."));
			Assert.That(manager.MessageBoxes[1], Does.Contain("Отправка заказов завершена успешно."));
			Assert.That(command, Is.InstanceOf<SendOrders>());
		}

		[Test]
		public void Show_about()
		{
			var about = manager.DialogOpened.OfType<About>().ToValue();
			shell.ShowAbout();
			Assert.IsNotNull(about.Value);
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

			shell.UpdateStat();
			var canClose = new Subject<bool>();
			var last = canClose.Take(1).PublishLast();
			last.Connect();
			shell.CanClose(b => canClose.OnNext(b));
			Assert.IsTrue(last.Timeout(TimeSpan.FromSeconds(3)).First());

			session.Evict(order);
			var reloaded = session.Get<SentOrder>(order.Id);
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
			SettingsViewModel settingsModel = null;
			manager.DialogOpened.OfType<SettingsViewModel>().Subscribe(x => {
				settingsModel = x;
				x.Settings.Value.OpenRejects = !x.Settings.Value.OpenRejects;
				var messages = manager.MessageOpened.Collect();
				var results = x.Save().ToList();
				Assert.IsEmpty(messages.Implode());
				Close(x);
			});
			shell.ShowSettings();

			scheduler.AdvanceByMs(1000);
			Assert.AreEqual(settingsModel.Settings.Value.OpenRejects,
				shell.Settings.Value.OpenRejects);
		}

		[Test]
		public void Repeat_request_on_unathorized_exception()
		{
			restore = true;
			var settings = false;
			stub.Do = c => { throw new RequestException("Unauthorized", HttpStatusCode.Unauthorized); };
			manager.DialogOpened.Subscribe(d => {
				if (d is WaitViewModel)
					((WaitViewModel)d).Closed.WaitOne();
				else {
					settings = true;
					stub.Do = c => c.result;
					var model = (SettingsViewModel)d;
					model.Settings.Value.Password = "aioxct2";
					var results = model.Save().ToList();
					Close(model);
				}
			});
			shell.Update();

			Assert.That(manager.MessageBoxes.Implode(), Does.Contain("Введены некорректные учетные данные"));
			Assert.IsTrue(settings);
			Assert.That(manager.MessageBoxes.Implode(), Does.Contain("Обновление завершено успешно"));
			Assert.AreEqual("test", ((NetworkCredential)stub.Handler.Credentials).UserName);
			Assert.AreEqual("aioxct2", ((NetworkCredential)stub.Handler.Credentials).Password);
		}

		[Test]
		public void Show_correction()
		{
			restore = true;
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
			Assert.IsFalse(correction.IsOrderSend);
			order = session.Query<Order>().First();
			scheduler.AdvanceByMs(200);
			//тк мы оперируем случайными данными то мы можем изменить OfferId заказанной позиции если
			//все остальные атрибуты совпали а цена у нее ниже
			Assert.That(correction.Offers.Value.Count, Is.GreaterThan(0));
			var offer = correction.Offers.Value.FirstOrDefault(o => o.Id == order.Lines[0].OfferId);
			Assert.IsNotNull(offer);
			Assert.AreEqual(1, offer.OrderCount,
				String.Format("рассматриваемый offerId = {0}, существующие = {1}",
					order.Lines[0].OfferId,
					session.Query<OrderLine>().Implode(l => $"offerId = {l.OfferId}, ошибка = {l.SendError}")));
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
			scheduler.Start();
			Assert.IsNotNull(main.Newses.Value[0].Url);
		}

		[Test]
		public void Do_not_warn_on_not_send_orders()
		{
			session.DeleteEach<Order>();
			//форма инициализируется лениво и при инициализации все заказы отмечаются для отправки
			Assert.IsNotNull(shell.Addresses);

			var offer = session.Query<Offer>().First(o => o.Price.SupplierName.Contains("минимальный заказ"));
			var order1 = MakeOrder(offer);
			order1.Send = false;
			MakeOrder(session.Query<Offer>().First(o => !o.Price.SupplierName.Contains("минимальный заказ")));

			var result = shell.SendOrders().ToArray();
			Assert.AreEqual(0, result.Length, result.Implode());
		}

		[Test]
		public void Recalculate_leaders_on_start()
		{
			restore = true;
			settings.LastUpdate = DateTime.Now;
			settings.LastLeaderCalculation = DateTime.Today.AddDays(-1);
			user.IsDelayOfPaymentEnabled = true;

			session.DeleteEach<DelayOfPayment>();
			var offer = session.Query<Offer>()
				.First(o => (o.LeaderPrice.Id.PriceId != o.Price.Id.PriceId || o.LeaderPrice == null) && !o.VitallyImportant && !o.Junk);
			//в прайс-листе может быть несколько предложений нам нужно выбрать самое дешевое
			offer = session.Query<Offer>()
				.Where(o => o.Price == offer.Price && o.ProductId == offer.ProductId && !o.VitallyImportant && !o.Junk)
				.OrderBy(o => o.Cost)
				.First();

			var delay = session.Query<DelayOfPayment>()
				.FirstOrDefault(d => d.DayOfWeek == DateTime.Today.DayOfWeek && d.Price == offer.Price)
				?? new DelayOfPayment(-99.999m, offer.Price);
			delay.OtherDelay = -99.999m;
			delay.VitallyImportantDelay = -99.999m;
			session.Save(delay);
			session.Flush();

			manager.DialogOpened.OfType<WaitViewModel>().Subscribe(m => m.Closed.WaitOne());
			shell.OnViewReady().Each(r => {
				if (r is TaskResult) {
					var task = ((TaskResult)r).Task;
					if (task.Status == TaskStatus.Created)
						task.Start();
					if (!task.Wait(30.Second()))
						throw new Exception("Не удалось дождаться задачи за 30 секунд");
				} else {
					r.Execute(new ActionExecutionContext());
				}
			});
			Close(shell);

			session.Refresh(offer);
			session.Refresh(offer.Price);
			Assert.AreEqual(offer.Price, offer.LeaderPrice, offer.Id.ToString());
			session.Refresh(settings);

			Assert.AreEqual(DateTime.Today.Date, settings.LastLeaderCalculation.Date);

			var minCost = session.Query<MinCost>().First(m => m.ProductId == offer.ProductId);
			Assert.AreEqual(offer.ResultCost, minCost.Cost, offer.Id.ToString());
			Assert.IsNotNull(minCost.NextCost, offer.Id.ToString());
			Assert.IsNotNull(minCost.Diff, minCost.ToString());
			Assert.That(minCost.Diff, Is.GreaterThan(0), minCost.ToString());
		}

		[Test]
		public void Attach_progress()
		{
			Execute.ResetWithoutDispatcher();
			var ready = new ManualResetEvent(false);
			var done = new ManualResetEvent(false);
			stub.Do = c => {
				ready.WaitOne();
				scheduler.AdvanceByMs(400);
				c.Reporter.Stage("1");
				scheduler.AdvanceByMs(400);
				c.Reporter.Progress();
				scheduler.AdvanceByMs(400);
				c.Reporter.Stage("2");
				scheduler.AdvanceByMs(400);
				done.Set();
				return c.result;
			};
			var events = new List<Progress>();
			manager.DialogOpened.OfType<SyncViewModel>().Subscribe(m => {
				m.Progress.Skip(1).Collect(events);
				ready.Set();
			});
			shell.Update();

			done.WaitOne(10.Second());
			Assert.AreEqual(4, events.Count, events.Implode());
		}

		[Test]
		public void Reload_data_on_reject()
		{
			//тест на ошибку, после обновления если мы отображаем PostUpdate
			//мы должны обновить данные в shell иначе последующие действия приведут к ошибкам
			stub = null;
			Fixture<LocalAddress>();
			Fixture<RejectedWaybill>();

			Collect(shell.Update());
			Assert.IsInstanceOf<PostUpdate>(dialogs[0]);
			Assert.AreEqual(1, shell.Addresses.Count);
		}

		[Test]
		public void Mark_for_send_on_init()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();
			order.Send = false;

			Assert.AreEqual(0, shell.Stat.Value.ReadyForSendOrdersCount);
			scheduler.Start();
			Assert.AreEqual(1, shell.Stat.Value.ReadyForSendOrdersCount);
			session.Refresh(order);
			Assert.IsTrue(order.Send);
		}

		[Test]
		public void Activate_stock_view()
		{
			shell.ShowInventory();
			Assert.AreEqual(5, shell.PrintMenuItems.Value.Count);
			shell.Checks();
			Assert.AreEqual(3, shell.PrintMenuItems.Value.Count);
		}

		private void Collect(IEnumerable<IResult> results)
		{
			dialogs.AddRange(results.OfType<DialogResult>().Select(d => d.Model));
			foreach (var dialog in dialogs.OfType<BaseScreen>()) {
				dialog.Shell = shell;
			}
		}
	}
}