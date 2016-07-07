using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Main : Conductor<IScreen>
	{
		public Main()
		{
			DisplayName = "Склад";
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			if (ActiveItem == null)
				Stocks();
		}

		public void Stocks()
		{
			ActiveItem = new Stocks();
		}

		public void ReceivingOrders()
		{
			ActiveItem = new ReceivingOrders(this);
		}

		public static IScreen Navigate(uint? receivingOrderId)
		{
			return new Main {
				ActiveItem = new ReceivingDetails(receivingOrderId.Value)
			};
		}
	}
}