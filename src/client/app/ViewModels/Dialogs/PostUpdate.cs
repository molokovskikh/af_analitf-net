
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PostUpdate : Screen
	{
		public PostUpdate(bool isRejected, bool isAwaited)
		{
			DisplayName = "АналитФАРМАЦИЯ: Внимание";
			Header = "Внимание!";
			Text = "Обновление завершено успешно.\r\n" +
				"Обнаружены следующие события:";
			IsAwaited = isAwaited;
			IsRejected = isRejected;
			if (isRejected)
				Text += "\r\n  - обнаружены препараты, предписанные к изъятию, в имеющихся у Вас электронных накладных";
			if (isAwaited)
				Text += "\r\n  - появились препараты, которые включены Вами в список ожидаемых позиций";
		}

		public ShellViewModel Shell { get; set; }
		public string Header { get; set; }
		public string Text { get; set; }
		public bool IsRejected { get; set; }
		public bool IsAwaited { get; set; }

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