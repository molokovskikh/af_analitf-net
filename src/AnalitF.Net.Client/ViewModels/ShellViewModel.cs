using System.Collections;
using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels
{
	public class ShellViewModel : Conductor<IScreen>
	{
		private Stack<IScreen> screens = new Stack<IScreen>();

		public ShellViewModel()
		{
			DisplayName = "АналитФАРМАЦИЯ";
		}

		public void ShowCatalog()
		{
			ActivateItem(new CatalogViewModel());
		}

		public void ShowPrice()
		{
			ActivateItem(new PriceViewModel());
		}

		public void ShowMnn()
		{
			ActivateItem(new MnnViewModel());
		}

		public void ActiveAndSaveCurrent(IScreen item)
		{
			if (ActiveItem != null) {
				screens.Push(ActiveItem);
				DeactivateItem(ActiveItem, false);
			}

			ActivateItem(item);
		}

		public override void DeactivateItem(IScreen item, bool close)
		{
			base.DeactivateItem(item, close);

			if (ActiveItem == null && screens.Count > 0) {
				ActivateItem(screens.Peek());
			}
		}
	}
}