using System.Collections.Generic;
using System.IO;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class TextViewModel : Screen
	{
		public TextViewModel(string text)
		{
			DisplayName = "Не найденные позиции";
			Text = text;
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

			using(var f = new StreamWriter(File.OpenWrite(saveFileResult.Dialog.FileName))) {
				f.Write(Text);
			}
		}
	}
}