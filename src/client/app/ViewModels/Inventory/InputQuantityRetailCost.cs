using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels.Parts;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class InputQuantityRetailCost : BaseScreen
	{
		public InputQuantityRetailCost(BarcodeProducts barcodeProduct)
		{
			InitFields();
			BarcodeProduct.Value = barcodeProduct;
			WasCancelled = true;
		}

		public NotifyValue<uint?> Quantity { get; set; }
		public NotifyValue<decimal?> RetailCost { get; set; }
		public NotifyValue<BarcodeProducts> BarcodeProduct { get; set; }
		public InlineEditWarning Warning { get; set; }
		public bool WasCancelled { get; set; }

		public void OK()
		{
			if (Quantity.Value == null)
			{
				Warning.Show(Common.Tools.Message.Warning($"Не указано количество"));
				return;
			}
			if (RetailCost.Value == null)
			{
				Warning.Show(Common.Tools.Message.Warning($"Не указано розничная цена"));
				return;
			}
			WasCancelled = false;
			TryClose();
		}

		public void Committed()
		{
		}

		public override void TryClose()
		{
			Committed();
			base.TryClose();
		}
	}
}
