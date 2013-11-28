using System;
using System.IO;
using Caliburn.Micro;
using Microsoft.Win32;

namespace AnalitF.Net.Client.Models.Results
{
	public class SaveFileResult : IResult
	{
		private log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SaveFileResult));

		public SaveFileDialog Dialog = new SaveFileDialog();
		public Extentions.WindowManager Manager;

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
			try
			{
				using(var f = new StreamWriter(Dialog.FileName)) {
					f.Write(text);
				}
			}
			catch(UnauthorizedAccessException e) {
				Report(e);
			}
			catch(IOException e) {
				Report(e);
			}
		}

		private void Report(Exception e)
		{
			logger.Warn("Ошибка при сохранении файла", e);
			Manager.Error(e.Message);
		}
	}
}