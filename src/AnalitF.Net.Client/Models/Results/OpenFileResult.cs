using System;
using System.Windows.Forms;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class OpenFileResult : IResult
	{
		public OpenFileDialog Dialog = new OpenFileDialog();
		public event EventHandler<ResultCompletionEventArgs> Completed;

		public void Execute(ActionExecutionContext context)
		{
			var result = Dialog.ShowDialog();
			if (Completed != null) {
				var resultCompletionEventArgs = new ResultCompletionEventArgs {
					WasCancelled = result != System.Windows.Forms.DialogResult.OK || String.IsNullOrEmpty(Dialog.FileName)
				};
				Completed(this, resultCompletionEventArgs);
			}
		}
	}
}