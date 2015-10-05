using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Common.Tools.Calendar;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class SelfClose : Screen
	{
		private string countdown;
		private int seconds;
		private CancellationDisposable closeDisposable = new CancellationDisposable();

		public IScheduler Scheduler;

		public SelfClose(string request, string caption, int seconds)
		{
			Message = request;
			DisplayName = caption;
			countdown =  caption + " будет произведено через {0} секунд";
			this.seconds = seconds;
			CountDown = new NotifyValue<string>();
		}

		public string Message { get; set; }
		public NotifyValue<string> CountDown { get; set; }

		protected override void OnActivate()
		{
			var countDown = Observable.Timer(TimeSpan.Zero, 1.Second(), Scheduler)
				.Select(v => seconds - v)
				.TakeWhile(v => v >= 0);
			countDown.Subscribe(i => CountDown.Value = String.Format(countdown, i), closeDisposable.Token);
			countDown.Where(v => v == 0).Subscribe(_ => TryClose(), closeDisposable.Token);
		}

		protected override void OnDeactivate(bool close)
		{
			if (close)
				closeDisposable.Dispose();
		}
	}
}