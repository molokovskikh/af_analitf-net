using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class ShellFixture : BaseUnitFixture
	{
		public class ExportScreen : Screen, IExportable
		{
			public ExportScreen()
			{
				CanExport = new NotifyValue<bool>();
			}

			public NotifyValue<bool> CanExport { get; set; }

			public IResult Export()
			{
				throw new NotImplementedException();
			}
		}

		Subject<RemoteCommand> cmd;

		[SetUp]
		public void Setup()
		{
			cmd = new Subject<RemoteCommand>();
			shell.Settings.Value = new Settings();
			shell.CommandExecuting += command => {
				cmd.OnNext(command);
				return new StubRemoteCommand(UpdateResult.OK);
			};
			shell.ResultsSink.Subscribe(r => {
				if (r is DialogResult) {
					((DialogResult)r).RaiseCompleted(new ResultCompletionEventArgs());
				}
				else {
					r.Execute(new ActionExecutionContext());
				}
			});
		}

		[TearDown]
		public void Teardown()
		{
			SystemTime.Reset();
		}

		[Test]
		public void Start_scheduled_update()
		{
			shell.Settings.Value.UserName = "test";
			shell.Settings.Value.Password = "password";
			shell.Schedules.Value = new List<Schedule> {
				new Schedule(new TimeSpan(18, 0, 0))
			};
			var result = cmd.Collect();
			SystemTime.Now = () => new DateTime(2014, 03, 20, 19, 5, 0);
			scheduler.AdvanceByMs(30000);
			Assert.IsInstanceOf<UpdateCommand>(result[0]);
		}

		[Test]
		public void Do_not_update_if_schedule_not_meet()
		{
			var result = cmd.Collect();
			shell.Settings.Value.LastUpdate = new DateTime(2014, 03, 20, 12, 5, 0);
			shell.Settings.Value.UserName = "test";
			shell.Settings.Value.Password = "password";
			shell.Schedules.Value = new List<Schedule> {
				new Schedule(new TimeSpan(20, 0, 0))
			};
			SystemTime.Now = () => new DateTime(2014, 03, 20, 19, 5, 0);
			scheduler.AdvanceByMs(60000);
			Assert.IsEmpty(result);
		}

		[Test]
		public void Periodical_check_schedule()
		{
			var result = cmd.Collect();

			shell.Settings.Value.UserName = "test";
			shell.Settings.Value.Password = "password";
			shell.Schedules.Value = new List<Schedule> {
				new Schedule(new TimeSpan(18, 0, 0))
			};
			SystemTime.Now = () => new DateTime(2014, 03, 20, 19, 5, 0);
			scheduler.AdvanceByMs(30000);
			Assert.IsInstanceOf<UpdateCommand>(result[0]);

			//если обновление не удалось мы должны попытаться еще раз
			result.Clear();
			scheduler.AdvanceByMs(30000);
			Assert.IsInstanceOf<UpdateCommand>(result[0]);
		}

		[Test]
		public void Do_not_close_subject()
		{
			var result = cmd.Collect();

			shell.Settings.Value.UserName = "test";
			shell.Settings.Value.Password = "password";
			shell.Schedules.Value = new List<Schedule>();
			scheduler.AdvanceByMs(30000);
			Assert.IsEmpty(result);

			shell.Schedules.Value = new List<Schedule> {
				new Schedule(new TimeSpan(18, 0, 0))
			};
			SystemTime.Now = () => new DateTime(2014, 03, 20, 19, 5, 0);
			scheduler.AdvanceByMs(30000);
			Assert.IsInstanceOf<UpdateCommand>(result[0]);
		}

		[Test]
		public void Track_can_export_changes()
		{
			var canExport = false;
			shell.CanExport.Changed().Subscribe(_ => canExport = shell.CanExport.Value);
			var export = new ExportScreen();
			shell.ActivateItem(export);

			export.CanExport.Value = true;

			Assert.IsTrue(canExport);
		}

		[Test]
		public void Do_not_export_unexportable()
		{
			var canExport = false;
			shell.CanExport.Changed().Subscribe(_ => canExport = shell.CanExport.Value);
			var export = new ExportScreen();
			export.CanExport.Value = true;
			shell.ActivateItem(export);
			Assert.IsTrue(canExport);
			shell.ActivateItem(new Screen());
			Assert.IsFalse(canExport);
		}

		[Test]
		public void Do_not_warn_on_not_send_orders()
		{
			shell.CurrentAddress = new Address("тест");
			var line = shell.CurrentAddress.Order(new Offer(new Price("тест"), 50), 1);
			line.Order.Send = false;
			bus.SendMessage(new Stat(shell.CurrentAddress));
			scheduler.Start();
			shell.TryClose();
			Assert.AreEqual("", manager.MessageBoxes.Implode());
		}

		[Test]
		public void Run_batch()
		{
			var cmds = cmd.Collect();

			shell.CurrentAddress = new Address("тест");
			shell.Addresses.Add(shell.CurrentAddress);
			shell.Settings.Value.LastUpdate = DateTime.Now;
			shell.Settings.Value.UserName = "test";
			shell.Settings.Value.Password = "password";
			shell.Config.Cmd = "batch=1.txt";

			var results = shell.OnViewReady().ToArray();
			Assert.AreEqual(1, cmds.Count);
			Assert.AreEqual("1.txt", ((UpdateCommand)cmds[0]).BatchFile);
		}
	}
}