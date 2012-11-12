using System.Threading;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaitCancelViewModel : BaseScreen
	{
		private CancellationTokenSource cancellation;

		public WaitCancelViewModel(CancellationTokenSource cancellation)
		{
			this.cancellation = cancellation;
			Text = "Производится обмен данными.\r\nПожалуйста подождите.";
		}

		public string Text { get; set; }

		public void Cancel()
		{
			cancellation.Cancel();
		}
	}
}