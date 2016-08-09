using System;
using System.Threading;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class WaitViewModel : Screen
	{
		public WaitViewModel()
		{
			DisplayName = "АналитФАРМАЦИЯ";
			Text = "Выполнение операции, подождите.";
			GenericErrorMessage = "Не удалось выполнить операцию. Обратитесь в АналитФармация.";
			Cancellation = new CancellationTokenSource();
		}

		public WaitViewModel(string text)
			: this()
		{
			Text = text;
		}

		public AutoResetEvent Closed = new AutoResetEvent(false);

		public CancellationTokenSource Cancellation;
		public string Text { get; set; }
		public bool IsCompleted { get; set; }
		public string GenericErrorMessage { get; set; }

		public void Cancel()
		{
			Cancellation.Cancel();
		}

		public override void TryClose()
		{
			base.TryClose();
			Closed.Set();
		}

		public override void CanClose(Action<bool> callback)
		{
			if (!IsCompleted) {
				//мы не можем закрываться тк происходит процесс обмена данными
				//все что мы можем попытаться отменить его и ждать пока, процесс будет отменен и сам закроет диалог
				Cancel();
			}
			callback(IsCompleted);
		}
	}
}