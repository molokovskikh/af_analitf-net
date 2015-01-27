using System;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class SyncViewModel : WaitViewModel
	{
		public SyncViewModel(IObservable<Progress> progress)
		{
			Text = "Производится обмен данными.\r\nПожалуйста подождите.";
			DisplayName = "Обмен данными";
			Time = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), RxApp.DeferredScheduler)
				.Select(v => TimeSpan.FromSeconds(v))
				.ToValue();
			Progress = progress.ToValue();
		}

		public NotifyValue<TimeSpan> Time { get; set; }
		public NotifyValue<Progress> Progress { get; set; }
	}
}