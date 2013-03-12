using System;
using Caliburn.Micro;
using Microsoft.Win32;

namespace AnalitF.Net.Client.Models.Results
{
	public class SaveFileResult : IResult
	{
		public SaveFileDialog Dialog = new SaveFileDialog();

		public SaveFileResult()
		{
		}

		public void Execute(ActionExecutionContext context)
		{
			var result = Dialog.ShowDialog();
			if (Completed != null) {
				var resultCompletionEventArgs = new ResultCompletionEventArgs {
					WasCancelled = !result.GetValueOrDefault() || String.IsNullOrEmpty(Dialog.SafeFileName)
				};
				Completed(this, resultCompletionEventArgs);
			}
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}