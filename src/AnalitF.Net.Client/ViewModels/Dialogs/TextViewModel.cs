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
		private WindowManager manager;

		public TextViewModel(string text)
		{
			DisplayName = "Не найденные позиции";
			Text = text;
			manager = (Extentions.WindowManager)IoC.Get<IWindowManager>();
		}

		public string Text { get; set; }

		public IEnumerable<IResult> Save()
		{
			var saveFileResult = new SaveFileResult {
				Dialog = {
					DefaultExt = ".txt",
					Filter = "Текстовые файлы (.txt)|*.txt"
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
			manager.Error(e.Message);
		}
	}
}