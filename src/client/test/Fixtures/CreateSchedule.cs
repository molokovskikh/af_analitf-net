using System;
using System.Linq;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;
using Test.Support;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateSchedule : ServerFixture
	{
		public TimeSpan[] Schedules = new TimeSpan[0];

		public override void Execute(ISession session)
		{
			var user = User(session);
			user.Client.Settings.AllowAnalitFSchedule = true;
			if (Verbose && Schedules.Length == 0) {
				Console.Write("Ввидите время автоматического обновления:");
				Schedules = new[] { TimeSpan.Parse(Console.ReadLine()) };
			}

			session.DeleteEach(session.Query<TestAnalitFSchedule>().Where(s => s.Client == user.Client));

			foreach (var schedule in Schedules) {
				session.Save(new TestAnalitFSchedule(user.Client, schedule));
			}
		}

		public override void Rollback(ISession session)
		{
			var user = User(session);
			user.Client.Settings.AllowAnalitFSchedule = false;
		}
	}
}