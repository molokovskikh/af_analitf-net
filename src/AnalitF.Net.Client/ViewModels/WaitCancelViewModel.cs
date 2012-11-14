using System;
using System.Reactive.Linq;
using System.Threading;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class Progress
	{
		public string Stage { get; set; }
		public int Current { get; set; }
		public int Total { get; set; }

		public Progress()
		{
		}

		public Progress(string stage, int current, int total)
		{
			Stage = stage;
			Current = current;
			Total = total;
		}
	}

	public class WaitCancelViewModel : BaseScreen
	{
		private CancellationTokenSource _cancellation;
		private TimeSpan _time;
		private Progress _progress;

		public WaitCancelViewModel(CancellationTokenSource cancellation, IObservable<Progress> progress)
		{
			DisplayName = "Обмен данными";
			Text = "Производится обмен данными.\r\nПожалуйста подождите.";
			_cancellation = cancellation;
			var timer = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), RxApp.DeferredScheduler)
				.Subscribe(t => Time = TimeSpan.FromSeconds(t));
			progress.Subscribe(p => Progress = p);
		}

		public string Text { get; set; }

		public TimeSpan Time
		{
			get { return _time; }
			set
			{
				_time = value;
				RaisePropertyChangedEventImmediately("Time");
			}
		}

		public Progress Progress
		{
			get { return _progress; }
			set
			{
				_progress = value;
				NotifyOfPropertyChange("Progress");
			}
		}

		public void Cancel()
		{
			_cancellation.Cancel();
		}
	}
}