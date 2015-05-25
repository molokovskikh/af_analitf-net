using System;
using System.Collections.Generic;
using System.IO;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class TextViewModel : Screen
	{
		protected TextViewModel()
		{
		}

		public TextViewModel(string text)
		{
			DisplayName = "Ненайденные позиции";
			Header = "Предложения по данным позициям из заказа отсутствуют:";
			Text = text;
		}

		public string Header { get; set; }
		public string Text { get; set; }

		public IEnumerable<IResult> Save()
		{
			var saveFileResult = new SaveFileResult(DisplayName);
			yield return saveFileResult;
			saveFileResult.Write(Text);
		}
	}
}