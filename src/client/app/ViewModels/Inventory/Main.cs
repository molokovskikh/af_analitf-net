using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Main : Conductor<IScreen>
	{
		public Main()
		{
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