using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Text;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	class ReportSetting: BaseScreen2, ICancelable
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
			Printers = new ObservableCollection<string>();
      LocalPrintServer server = new LocalPrintServer();
			var printQueues =
				server.GetPrintQueues(new EnumeratedPrintQueueTypes[]
				{EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections});
      foreach (var printQueue in printQueues)
      {
          var printerWrapper =  printQueue.Name;
          Printers.Add(printerWrapper);
      }
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
