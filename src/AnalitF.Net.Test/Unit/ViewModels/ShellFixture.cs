using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Linq.Observαble;
using System.Reactive.Subjects;
using System.Text;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
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
		ShellViewModel shell;
		Subject<RemoteCommand> cmd;

		[SetUp]
		public void Setup()
		{
			cmd = new Subject<RemoteCommand>();
			shell = new ShellViewModel(true);
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
			shell.Settings.Value.LastUpdate = new DateTime(2014, 03, 20, 12, 5, 0);
			shell.Settings.Value.UserName = "test";
			shell.Settings.Value.Password = "password";
			shell.Schedules.Value = new List<Schedule> {
				new Schedule(new TimeSpan(20, 0, 0))
			};
			var result = cmd.Collect();
			SystemTime.Now = () => new DateTime(2014, 03, 20, 19, 5, 0);
			scheduler.AdvanceByMs(60000);
			Assert.IsEmpty(result);
		}

		[Test]
		public void Periodical_check_schedule()
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

			//если обновление не удалось мы должны попытаться еще раз
			result.Clear();
			scheduler.AdvanceByMs(30000);
			Assert.IsInstanceOf<UpdateCommand>(result[0]);
		}
	}
}