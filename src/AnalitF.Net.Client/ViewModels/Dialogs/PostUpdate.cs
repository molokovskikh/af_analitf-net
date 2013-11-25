
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PostUpdate : Screen
	{
		public PostUpdate()
		{
			DisplayName = "АналитФАРМАЦИЯ: Внимание";
			Header = "Внимание!";
			Text = "Обнаружены препараты, предписанные к изъятию, в имеющихся у Вас электронных накладных.";
		}

		public ShellViewModel Shell { get; set; }
		public string Header { get; set; }
		public string Text { get; set; }

		public void Show()
		{
			Shell.ShowRejectedWaybills();
			TryClose();
		}
	}
}