﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Caliburn.Micro;
using Common.Tools;
using Microsoft.Win32;

namespace AnalitF.Net.Client.Models.Results
{
	public class SaveFileResult : IResult
	{
		private log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SaveFileResult));

		public SaveFileDialog Dialog = new SaveFileDialog();
		public Extentions.WindowManager Manager;

		public SaveFileResult(Tuple<string, string>[] formats, string filename = null)
		{
			Dialog.FileName = filename;
			Dialog.DefaultExt = formats.Select(t => t.Item2).FirstOrDefault();
			Dialog.Filter = formats.Implode(k => String.Format("{0}|*{1}", k.Item1, k.Item2), "|");
		}

		public SaveFileResult(string defaultFilename = null)
		{
			Dialog.FileName = String.Format("{0} от {1:d}.txt", defaultFilename, DateTime.Now);
			Dialog.DefaultExt = ".txt";
			Dialog.Filter = "Текстовые файлы|*.txt";
		}

		public void Execute(ActionExecutionContext context)
		{
			var result = Dialog.ShowDialog();
			if (Completed != null) {
				var resultCompletionEventArgs = new ResultCompletionEventArgs {
					WasCancelled = !result.GetValueOrDefault() || String.IsNullOrEmpty(Dialog.SafeFileName)
				};
				Completed(this, resultCompletionEventArgs);
			}
		}

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public void Write(string text)
		{
			using(var f = Writer()) {
				f.Write(text);
			}
		}

		private void Report(Exception e)
		{
			Manager.Error(e.Message);
		}

		public StreamWriter Writer()
		{
			try {
				return new StreamWriter(Dialog.FileName, false, Encoding.Default);
			}
			catch(UnauthorizedAccessException e) {
				Report(e);
				throw;
			}
			catch(IOException e) {
				Report(e);
				throw;
			}
		}
	}
}