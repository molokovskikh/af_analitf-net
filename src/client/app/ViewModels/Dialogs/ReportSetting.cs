using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	internal class ReportSetting : BaseScreen2, ICancelable
	{
		public bool WasCancelled { get; private set; }
		public bool IsView { get; set; }
		public bool IsPrint { get; set; }
		public string PrinterName { get; set; }
		public ObservableCollection<string> Printers { get; set; }

		public ReportSetting()
		{
			DisplayName = "Настройки отчетов";
			WasCancelled = true;
			IsView = true;
			Printers = new ObservableCollection<string>(PrintHelper.GetPrinters().Select(x => x.Name));
			Printers.Add("");
		}

		public void Save()
		{
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			TryClose();
		}
	}
}
