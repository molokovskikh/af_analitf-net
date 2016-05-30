using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Main : Conductor<IScreen>
	{
		public Main()
		{
			ActiveItem = new Stocks();
		}
	}
}