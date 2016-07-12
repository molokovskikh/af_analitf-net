using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class InventoryAction : Screen, ICancelable
	{
		public InventoryAction()
		{
			WasCancelled = true;
			DisplayName = "Оприходование";
		}

		public bool New;
		public bool WasCancelled { get; set; }

		public void CreateNew()
		{
			New = true;
			WasCancelled = false;
			TryClose();
		}

		public void SelectExisting()
		{
			WasCancelled = false;
			TryClose();
		}
	}
}