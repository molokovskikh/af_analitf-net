using System;
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

		//TODO: Обработка ошибок?
		public void Execute(ActionExecutionContext context)
		{
			ProcessHelper.Start(new ProcessStartInfo(Filename) { Verb = "Open" });
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}