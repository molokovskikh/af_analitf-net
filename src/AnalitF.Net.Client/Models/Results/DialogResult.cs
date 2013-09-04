using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
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

		public bool FullScreen { get; set; }

		public void Execute(ActionExecutionContext context)
		{
			var windowManager = ((BaseScreen)context.Target).Manager;
			var args = new ResultCompletionEventArgs();
			IDictionary<string, object> settings = null;
			if (FullScreen) {
				settings = new Dictionary<string, object> {
					{"WindowState", WindowState.Maximized}
				};
			}
			if (ShowFixed)
				args.WasCancelled = !windowManager.ShowFixedDialog(Model).GetValueOrDefault();
			else
				args.WasCancelled = !windowManager.ShowDialog(Model, null, settings).GetValueOrDefault();
			if (Completed != null)
				Completed(this, args);
		}
	}
}