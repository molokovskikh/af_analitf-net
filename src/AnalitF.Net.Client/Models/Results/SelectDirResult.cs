using System;
using System.IO;
using System.Windows.Forms;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class SelectDirResult : IResult
	{
		private string dir;

		public string Result;

		public SelectDirResult(string dir)
		{
			this.dir = dir;
			Result = dir;
		}

		public void Execute(ActionExecutionContext context)
		{
			var dialog = new FolderBrowserDialog {
				SelectedPath = dir
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				Result = dialog.SelectedPath;
			}
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}