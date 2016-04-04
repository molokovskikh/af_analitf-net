using System;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PostUpdate : Screen
	{
		public PostUpdate()
		{
			DisplayName = "АналитФАРМАЦИЯ: Внимание";
			Header = "Внимание!";
		}

		public ShellViewModel Shell { get; set; }
		public string Header { get; set; }
		public string Text
		{
			get
			{
				var text = "Обновление завершено успешно.\r\n" +
				"Обнаружены следующие события:";
				if (IsRejected)
					text += "\r\n  - обнаружены препараты, предписанные к изъятию, в имеющихся у Вас электронных накладных";
				if (IsAwaited)
					text += "\r\n  - появились препараты, которые включены Вами в список ожидаемых позиций";
				if (IsDocsReceived)
					text += "\r\n  - получены новые документы";
				return text;
			}
		}

		public bool IsRejected { get; set; }
		public bool IsAwaited { get; set; }
		public bool IsDocsReceived { get; set; }

		public bool IsMadeSenseToShow => IsRejected || IsAwaited || IsDocsReceived;

		public void ShowNewDocs()
		{
			Shell.ShowWaybills();
			TryClose();
		}

		public void ShowRejects()
		{
			Shell.ShowRejectedWaybills();
			TryClose();
		}

		public void ShowAwaited()
		{
			Shell.ShowAwaited();
			TryClose();
		}
	}
}