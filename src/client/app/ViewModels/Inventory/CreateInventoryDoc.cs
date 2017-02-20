using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CreateInventoryDoc : BaseScreen, ICancelable
	{
		public CreateInventoryDoc(InventoryDoc doc)
		{
			InitFields();
			Doc = doc;
			DisplayName = "Создание документа излишков";
			WasCancelled = true;
		}

		public InventoryDoc Doc { get; set; }
		public bool WasCancelled { get; private set; }

		public void OK()
		{
			if (!IsValide(Doc))
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}
