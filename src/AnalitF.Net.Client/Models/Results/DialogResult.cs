using System;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class DialogResult : IResult
	{
		public DialogResult(Screen model)
		{
			Model = model;
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public Screen Model { get; private set; }

		public bool ShowFixed { get; set; }

		public void Execute(ActionExecutionContext context)
		{
			var windowManager = ((BaseScreen)context.Target).Manager;
			var args = new ResultCompletionEventArgs();
			if (ShowFixed)
				args.WasCancelled = !windowManager.ShowFixedDialog(Model).GetValueOrDefault();
			else
				args.WasCancelled = !windowManager.ShowDialog(Model).GetValueOrDefault();
			if (Completed != null)
				Completed(this, args);
		}
	}
}