using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class SelfCloseFixture : BaseUnitFixture
	{
		[Test]
		public void Close_on_timeout()
		{
			var selfClose = new SelfClose("Закрыть форму?", "Закрытие", 30);
			selfClose.Scheduler = scheduler;

			var conductor = new Conductor<Screen>();
			selfClose.Parent = conductor;
			conductor.ActiveItem = selfClose;

			ScreenExtensions.TryActivate(selfClose);
			Assert.IsTrue(selfClose.IsActive);
			scheduler.AdvanceByMs(15 * 1000);
			Assert.AreEqual("Закрытие будет произведено через 16 секунд", selfClose.CountDown.Value);
			scheduler.AdvanceByMs(16 * 1000);
			Assert.IsFalse(selfClose.IsActive);
		}
	}
}