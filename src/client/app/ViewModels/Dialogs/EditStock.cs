using System;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class EditStock : BaseScreen2, ICancelable
	{
		private uint id;

		public enum Mode
		{
			EditQuantity,
			EditStock
		}

		public EditStock()
		{
			DisplayName = "Информация о товаре";
			WasCancelled = true;
		}

		public EditStock(Stock stock)
			: this()
		{
			Stock = stock;
		}

		public EditStock(uint id)
			: this()
		{
			Stock = Session.Get<Stock>(id);
		}

		public bool WasCancelled { get; private set; }
		public Stock Stock { get; set; }
		public Mode EditMode { get; set; }

		public void OK()
		{
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
