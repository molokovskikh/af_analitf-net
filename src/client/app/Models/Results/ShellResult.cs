using System;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class ShellResult : IResult
	{
		private Action<ShellViewModel> action;
		public ShellViewModel Shell;

		public ShellResult(Action<ShellViewModel> action)
		{
			this.action = action;
		}

		public void Execute(ActionExecutionContext context)
		{
			action(Shell);
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}