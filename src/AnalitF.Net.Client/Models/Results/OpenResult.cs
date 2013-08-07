using System;
using System.Diagnostics;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
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