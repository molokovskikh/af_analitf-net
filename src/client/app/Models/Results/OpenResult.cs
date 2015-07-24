using System;
using System.ComponentModel;
using System.Diagnostics;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class SelectResult : IResult
	{
		public string Filename;

		public SelectResult(string filename)
		{
			Filename = filename;
		}

		public void Execute(ActionExecutionContext context)
		{
			Process.Start("explorer.exe", String.Format("/select,{0}", Filename));
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}

	public class OpenResult : IResult
	{
		public string Filename;

		public OpenResult(string filename)
		{
			Filename = filename;
		}

		public void Execute(ActionExecutionContext context)
		{
			var selectWithShell = false;
			try {
				ProcessHelper.Start(new ProcessStartInfo(Filename) { Verb = "Open" });
			}
			catch(Win32Exception e) {
				//System.ComponentModel.Win32Exception (0x80004005):
				//Указанному файлу не сопоставлено ни одно приложение для выполнения данной операции
				//если нет сопоставленного приложения выбераем файл в shell
				if ((uint)e.NativeErrorCode == 0x80004005)
					selectWithShell = true;
			}
			if (selectWithShell)
				ProcessHelper.Start(new ProcessStartInfo("explorer.exe", String.Format("/select,{0}", Filename)));

			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}