using System;
using System.ComponentModel;
using System.Diagnostics;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class OpenResult : IResult
	{
		private log4net.ILog log = global::log4net.LogManager.GetLogger(typeof(OpenResult));

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
				//если нет сопоставленного приложения выбираем файл в shell
				if ((uint)e.NativeErrorCode == 0x80004005 || e.NativeErrorCode == 1155)
					selectWithShell = true;
				else
					log.Error($"Fail to open file {Filename}", e);
			}
			if (selectWithShell)
				ProcessHelper.Start(new ProcessStartInfo("explorer.exe", $"/select,{Filename}"));

			Completed?.Invoke(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}

	public class SelectResult : IResult
	{
		public string Filename;

		public SelectResult(string filename)
		{
			Filename = filename;
		}

		public void Execute(ActionExecutionContext context)
		{
			ProcessHelper.Start(new ProcessStartInfo("explorer.exe", $"/select,{Filename}"));

			Completed?.Invoke(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}