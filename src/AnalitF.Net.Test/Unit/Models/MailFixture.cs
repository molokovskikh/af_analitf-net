using System;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class MailFixture
	{
		private NotifyValue<Mail> current;
		private TestScheduler scheduler;

		[SetUp]
		public void Setup()
		{
			current = new NotifyValue<Mail>();
			scheduler = new TestScheduler();
			Mail.TrackIsNew(scheduler, current);
		}

		[Test]
		public void Track_new()
		{
			current.Value = new Mail();
			current.Value.IsNew = false;
			current.Value.IsNew = true;
			scheduler.AdvanceByMs(10000);
			Assert.IsTrue(current.Value.IsNew);
		}

		[Test]
		public void Track_new_after_reselect()
		{
			var mail = new Mail();
			current.Value = mail;
			current.Value.IsNew = false;
			current.Value.IsNew = true;
			scheduler.AdvanceByMs(10000);
			Assert.IsTrue(current.Value.IsNew);
			current.Value = null;
			current.Value = mail;
			scheduler.AdvanceByMs(10000);
			Assert.IsFalse(current.Value.IsNew);
		}
	}
}