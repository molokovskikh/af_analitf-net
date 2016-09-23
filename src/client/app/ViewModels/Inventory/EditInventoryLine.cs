using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class EditInventoryLine : BaseScreen2, ICancelable
	{
		private InventoryDocLine source;

		public EditInventoryLine(InventoryDocLine line)
		{
			WasCancelled = true;
			source = line;
			Line = source.Copy();
		}

		public InventoryDocLine Line { get; set; }
		public bool WasCancelled { get; set; }

		public void OK()
		{
			if (Line.Quantity.GetValueOrDefault() == 0) {
				Manager.Error("Количество должно быть заполнено");
				return;
			}
			if (Line.SupplierCostWithoutNds.GetValueOrDefault() == 0) {
				Manager.Error("Цена поставщика должна быть заполнена");
				return;
			}
			if (Line.RetailCost.GetValueOrDefault() == 0) {
				Manager.Error("Розничная цена должна быть заполнена");
				return;
			}
			source.CopyFrom(Line);
			WasCancelled = false;
			TryClose();
		}
	}
}