using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools.Calendar;
using Common.Tools.Threading;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class SingleInstanceFixture
	{
		public class TestInstance : SingleInstance
		{
			public bool ActivationResult;

			public TestInstance(string name) : base(name)
			{
			}

			protected override bool TryActivateProcessWindow()
			{
				return ActivationResult;
			}
		}

		private TaskFactory factory;
		private CompositeDisposable disposable;

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			factory = new TaskFactory(new ThreadPerTaskScheduler());
		}

		[SetUp]
		public void Setup()
		{
			disposable = new CompositeDisposable();
		}

		[TearDown]
		public void TearDown()
		{
			disposable.Dispose();
		}

		[Test]
		public void Wait_for_shutdown()
		{
			var single1 = new SingleInstance("test");
			single1.TryStartAndWait();
			single1.SignalShutdown();

			var single2 = new SingleInstance("test");
			var task = new Task<bool>(single2.TryStartAndWait);
			task.Start();

			Assert.IsTrue(single2.WaitShutdown.Wait(1.Second()));

			single1.Dispose();
			Assert.IsTrue(task.Wait(1.Second()));

			Assert.IsTrue(task.Result);
		}

		[Test]
		public void Block_second_instance()
		{
			var single1 = new SingleInstance("test");
			disposable.Add(single1);
			Assert.IsTrue(single1.TryStartAndWait());

			var single2 = new SingleInstance("test");
			disposable.Add(single2);
			Assert.Throws<EndUserError>(() => single2.TryStartAndWait());
		}

		[Test]
		public void Release_mutex_on_thread_exit()
		{
			var barier = new Barrier(2);
			var single1 = new SingleInstance("test");
			var single2 = new SingleInstance("test");
			var task1 = factory.StartNew(() => {
				using(single1) {
					Assert.IsTrue(single1.TryStartAndWait());
					single1.Wait();

					single1.SignalShutdown();
					barier.SignalAndWait();
					single2.WaitShutdown.Wait();
				}
			});

			var task2 = factory.StartNew(() => {
				using(single2) {
					barier.SignalAndWait();
					Assert.IsTrue(single2.TryStartAndWait());
				}
			});

			task1.Wait();
			task2.Wait();
		}

		[Test]
		public void Wait_for_activation_before_exit()
		{
			var single1 = new TestInstance("test");
			var single2 = new TestInstance("test");
			disposable.Add(single1);
			disposable.Add(single2);
			Assert.IsTrue(single1.TryStart());

			var task = factory.StartNew(() => single2.TryStart());
			Assert.IsTrue(single2.WaitStartup.Wait(1.Second()));
			single2.ActivationResult = true;
			single1.SignalStartup();

			Assert.IsFalse(task.Result);
		}
	}
}