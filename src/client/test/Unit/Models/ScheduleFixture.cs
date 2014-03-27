using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class ScheduleFixture
	{
		[TearDown]
		public void Teardown()
		{
			SystemTime.Reset();
		}

		[Test]
		public void IsOutdate()
		{
			SystemTime.Now = () => new DateTime(2013, 3, 20, 13, 0, 0);
			Assert.IsFalse(Schedule.IsOutdate(new Schedule[0], DateTime.MinValue));
			Assert.IsFalse(Schedule.IsOutdate(new[] { new Schedule(new TimeSpan(8, 0, 0)) }, new DateTime(2013, 3, 20, 12, 30, 1)));
			Assert.IsTrue(Schedule.IsOutdate(new[] { new Schedule(new TimeSpan(8, 0, 0)) }, new DateTime(2013, 3, 19, 12, 30, 1)));
			Assert.IsTrue(Schedule.IsOutdate(new[] { new Schedule(new TimeSpan(16, 0, 0)) }, new DateTime(2013, 3, 19, 12, 30, 1)));
			Assert.IsFalse(Schedule.IsOutdate(new[] { new Schedule(new TimeSpan(16, 0, 0)) }, new DateTime(2013, 3, 19, 17, 30, 1)));
			SystemTime.Now = () => new DateTime(2013, 3, 20, 16, 21, 0);
			Assert.IsTrue(Schedule.IsOutdate(new[] { new Schedule(new TimeSpan(16, 0, 0)), new Schedule(new TimeSpan(8, 0, 0)) }, new DateTime(2013, 3, 20, 12, 30, 1)));
			SystemTime.Now = () => new DateTime(2013, 3, 20, 7, 0, 0);
			Assert.IsTrue(Schedule.IsOutdate(new[] { new Schedule(new TimeSpan(8, 0, 0)) }, new DateTime(2013, 3, 18, 12, 30, 1)));
		}
	}
}