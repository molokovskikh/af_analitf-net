﻿using System;
using System.Windows.Forms;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Results
{
	public class NativeDialogResult<T> : IResult where T : CommonDialog
	{
		public T Dialog;
		public Extentions.WindowManager Manager;

		public NativeDialogResult(T dialog)
		{
			Dialog = dialog;
		}

		public void Execute(ActionExecutionContext context)
		{
			var resultCompletionEventArgs = new ResultCompletionEventArgs {
				WasCancelled = Manager.ShowDialog(Dialog) != System.Windows.Forms.DialogResult.OK
			};

			if (Completed != null)
				Completed(this, resultCompletionEventArgs);
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;
	}
}