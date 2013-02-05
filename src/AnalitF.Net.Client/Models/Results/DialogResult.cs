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

		public void Execute(ActionExecutionContext context)
		{
			((BaseScreen)context.Target).Manager.ShowDialog(Model);
			if (Completed != null)
				Completed(this, new ResultCompletionEventArgs());
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}