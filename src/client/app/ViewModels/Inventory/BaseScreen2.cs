using System;
using System.Reactive.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class BaseScreen2 : BaseScreen
	{
		public BaseScreen2()
		{
			InitFields();
		}

		protected void TrackDb(Type type)
		{
			Bus.Listen<string>("db").Where(x => x == type.Name)
				.Subscribe(_ => UpdateOnActivate = true, CloseCancellation.Token);
		}

		public override void Update()
		{
			DbReloadToken.Value = new object();
		}
	}
}