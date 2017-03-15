using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class PrintSetting : BaseScreen2, ICancelable
	{
		public bool WasCancelled { get; private set; }
		public bool IsView { get; set; }
		public bool IsPrint { get; set; }
		public string PrinterName { get; set; }
		public ObservableCollection<string> Printers { get; set; }

		public PrintSetting()
		{
			DisplayName = "Настройка печати";
			WasCancelled = true;
			Printers = new ObservableCollection<string>(PrintHelper.GetPrinters().Select(x => x.Name));
			Printers.Add("");
		}

		public PrintSetting(string printerName, bool isView) : this()
		{
			PrinterName = printerName;
			IsView = isView;
			IsPrint = !IsView;
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
