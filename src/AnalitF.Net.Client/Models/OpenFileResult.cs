using System;
using System.Diagnostics;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models
{
	public class OpenFileResult : IResult
	{
		public string Filename;

		public OpenFileResult(string filename)
		{
			Filename = filename;
		}

		//TODO: Обработка ошибок?
		public void Execute(ActionExecutionContext context)
		{
			Process.Start(new ProcessStartInfo(Filename) { Verb = "Open" });
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}