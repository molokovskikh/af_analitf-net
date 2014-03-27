﻿using System;
using Caliburn.Micro;
using Microsoft.Win32;

namespace AnalitF.Net.Client.Models.Results
{
	public class OpenFileResult : IResult
	{
		public OpenFileDialog Dialog = new OpenFileDialog();
		public event EventHandler<ResultCompletionEventArgs> Completed;
		public Extentions.WindowManager Manager;

		public void Execute(ActionExecutionContext context)
		{
			bool? result;
			if (Manager == null)
				result = Dialog.ShowDialog();
			else
				result = Manager.ShowDialog(Dialog);
			if (Completed != null) {
				var resultCompletionEventArgs = new ResultCompletionEventArgs {
					WasCancelled = !result.GetValueOrDefault(false) || String.IsNullOrEmpty(Dialog.FileName)
				};
				Completed(this, resultCompletionEventArgs);
			}
		}
	}
}