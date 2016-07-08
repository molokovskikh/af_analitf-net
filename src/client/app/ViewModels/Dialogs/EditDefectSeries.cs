using System;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class EditDefectSeries : BaseScreen2, ICancelable
	{
		private uint id;

		public EditDefectSeries()
		{
			DisplayName = "Проверить позицию";
			WasCancelled = true;
		}

		public EditDefectSeries(Stock stock)
		{
			Stock = stock;
		}

		public EditDefectSeries(uint id)
			: this()
		{
			Stock = Session.Get<Stock>(id);
		}

		public bool WasCancelled { get; private set; }
		public Stock Stock { get; set; }

		public void Ok()
		{
			Stock.RejectStatus = RejectStatus.ForceRejected;
			WasCancelled = false;
			TryClose();
		}

		public void Not()
		{
			Stock.RejectStatus = RejectStatus.ForceNoRejected;
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			if (id > 0)
				Session.Refresh(Stock);
			TryClose();
		}
	}
}
