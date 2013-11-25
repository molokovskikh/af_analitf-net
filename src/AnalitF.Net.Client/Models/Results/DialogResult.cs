using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models.Results
{
	public class DialogResult : IResult
	{
		public ShellViewModel Shell;
		public Screen Model;
		public bool ShowFixed;
		public bool FullScreen;

		public DialogResult(Screen model, bool fullScreen = false, bool @fixed = false)
		{
			Model = model;
			FullScreen = fullScreen;
			ShowFixed = @fixed;
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public void Execute(ActionExecutionContext context)
		{
			var manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
			var args = new ResultCompletionEventArgs();
			IDictionary<string, object> settings = null;
			if (FullScreen) {
				settings = new Dictionary<string, object> {
					{"WindowState", WindowState.Maximized}
				};
			}

			Util.SetValue(Model, "Shell", Shell);
			if (ShowFixed)
				args.WasCancelled = !manager.ShowFixedDialog(Model).GetValueOrDefault(true);
			else
				args.WasCancelled = !manager.ShowDialog(Model, null, settings).GetValueOrDefault(true);
			if (Completed != null)
				Completed(this, args);
		}

		public override string ToString()
		{
			return String.Format("Диалог - {0}", Model);
		}
	}
}