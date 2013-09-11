using System;
using System.Collections.Generic;
using System.IO;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using WindowManager = AnalitF.Net.Client.Extentions.WindowManager;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class TextViewModel : Screen
	{
		private log4net.ILog logger = log4net.LogManager.GetLogger(typeof(TextViewModel));

		protected TextViewModel()
		{
		}

		public TextViewModel(string text)
		{
			DisplayName = "Не найденные позиции";
			Header = "Предложения по данным позициям из заказа отсутствуют:";
			Text = text;
		}

		public string Header { get; set; }
		public string Text { get; set; }

		public IEnumerable<IResult> Save()
		{
			var saveFileResult = new SaveFileResult {
				Dialog = {
					DefaultExt = ".txt",
					Filter = "Текстовые файлы|*.txt"
				}
			};
			yield return saveFileResult;

			try
			{
				using(var f = new StreamWriter(File.OpenWrite(saveFileResult.Dialog.FileName))) {
					f.Write(Text);
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
			logger.Warn("Не критическая ошибка", e);
			var manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
			manager.Error(e.Message);
		}
	}
}