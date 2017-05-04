using System;
using System.Threading;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PrintProgressViewModel : Screen
	{		

		public PrintProgressViewModel(WindowManager windowManager, CancellationTokenSource cancellationToken)			
		{
			DisplayName = "АналитФАРМАЦИЯ";
			Text = "Выполняется печать, подождите.";
			this.windowManager = windowManager;
			this.cancellationToken = cancellationToken;
		}

		public void Start()
		{
			this.IsCompleted = false;
			this.windowManager.ShowWindow(this);
		}
		

		public void Finished()
		{
			this.IsCompleted = true;
			this.TryClose();
		}

		private WindowManager windowManager;

		public string Text { get; set; }
		private bool IsCompleted;
		private CancellationTokenSource cancellationToken;

		public void OnClickCancel(object sender, EventArgs e)
		{
			this.Cancel();
		}

		public void Cancel()
		{
			this.cancellationToken.Cancel();
		}

		public override void CanClose(Action<bool> callback)
		{
			if(!IsCompleted)
				this.Cancel();

			callback(IsCompleted);
		}
	}
}