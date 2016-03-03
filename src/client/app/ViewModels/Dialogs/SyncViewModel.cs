using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class SyncViewModel : WaitViewModel
	{
		public SyncViewModel(IObservable<Progress> progress, IScheduler scheduler)
		{
			Text = "Производится обмен данными.\r\nПожалуйста подождите.";
			DisplayName = "Обмен данными";
			Time = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), scheduler)
				.Select(v => TimeSpan.FromSeconds(v))
				.ToValue();
			Progress = progress.Sample(TimeSpan.FromMilliseconds(300), Env.Current.UiScheduler).ToValue();
		}

		public NotifyValue<TimeSpan> Time { get; set; }
		public NotifyValue<Progress> Progress { get; set; }
	}
}