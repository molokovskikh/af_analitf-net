using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class SyncViewModel : WaitViewModel
	{
		private CancellationDisposable disposable = new CancellationDisposable();

		public SyncViewModel(IObservable<Progress> progress, IScheduler scheduler)
		{
			Text = "Производится обмен данными.\r\nПожалуйста подождите.";
			DisplayName = "Обмен данными";
			Time = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), scheduler)
				.Select(v => TimeSpan.FromSeconds(v))
				.ToValue(disposable);
			Progress = progress.Sample(TimeSpan.FromMilliseconds(300), Env.Current.UiScheduler).ToValue(disposable);
		}

		public NotifyValue<TimeSpan> Time { get; set; }
		public NotifyValue<Progress> Progress { get; set; }

		public override void TryClose()
		{
			base.TryClose();
			disposable.Dispose();
		}
	}
}