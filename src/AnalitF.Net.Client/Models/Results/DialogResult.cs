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

		public Screen Model { get; private set; }

		public bool ShowFixed { get; set; }

		public void Execute(ActionExecutionContext context)
		{
			var windowManager = ((BaseScreen)context.Target).Manager;
			if (ShowFixed)
				windowManager.ShowFixedDialog(Model);
			else
				windowManager.ShowDialog(Model);
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}