using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class ScannerConfig : Screen, ICancelable
	{
		public ScannerConfig()
		{
			WasCancelled = true;
			DisplayName = "Тест сканера";
		}

		public int? Prefix { get; set; }
		public int? Sufix { get; set; }
		public bool WasCancelled { get; private set; }

		public void OK()
		{
			WasCancelled = false;
		}
	}
}