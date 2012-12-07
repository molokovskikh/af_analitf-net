using System;
using System.Threading;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaitViewModel : BaseScreen
	{
		public WaitViewModel()
		{
			Cancellation = new CancellationTokenSource();
		}

		public WaitViewModel(string text)
			: this()
		{
			Text = text;
			DisplayName = "АналитФАРМАЦИЯ";
			GenericErrorMessage = "Не удалось выполнить операцию. Обратитесь в АК\"Инфорум\".";
		}

		public CancellationTokenSource Cancellation;
		public string Text { get; set; }
		public bool IsCompleted { get; set; }

		public string GenericErrorMessage { get; set; }

		public void Cancel()
		{
			Cancellation.Cancel();
		}

		public override void CanClose(Action<bool> callback)
		{
			//мы не можем закрываться тк происходит процесс обмена данными
			//все что мы можем попытаться отменить его и ждать пока, процесс будет отменен и сам закроет диалог
			Cancel();
			callback(IsCompleted);
		}
	}
}