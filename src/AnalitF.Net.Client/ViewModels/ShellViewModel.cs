using System.Collections;
using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels
{
	public class ShellViewModel : Conductor<IScreen>
	{
		private Stack<IScreen> navigationChain = new Stack<IScreen>();

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

		public void Navigate(IScreen item)
		{
			if (ActiveItem != null) {
				navigationChain.Push(ActiveItem);
				DeactivateItem(ActiveItem, false);
			}

			ActivateItem(item);
		}

		public IEnumerable<IScreen> NavigationChain
		{
			get { return navigationChain; }
		}

		public override void DeactivateItem(IScreen item, bool close)
		{
			base.DeactivateItem(item, close);

			if (ActiveItem == null && navigationChain.Count > 0) {
				ActivateItem(navigationChain.Peek());
			}
		}

		public void CancelNavigation()
		{
			while (navigationChain.Count > 0) {
				var screen = navigationChain.Pop();
				screen.TryClose();
			}
		}

		public void PushInChain(IScreen screen)
		{
			navigationChain.Push(screen);
		}
	}
}