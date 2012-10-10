using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	[Serializable]
	public class ShellViewModel : Conductor<IScreen>//, ISerializable
	{
		private Stack<IScreen> navigationChain = new Stack<IScreen>();

		public ShellViewModel()
		{
			AppBootstrapper.Shell = this;
			DisplayName = "АналитФАРМАЦИЯ";
			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => UpdateDisplayName());
		}

		protected void UpdateDisplayName()
		{
			var value = "АналитФАРМАЦИЯ";
			var named =  ActiveItem as IHaveDisplayName;

			if (named != null && !String.IsNullOrEmpty(named.DisplayName)) {
				value += " - " + named.DisplayName;
			}
			DisplayName = value;
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

		public void SearchOffers()
		{
			ActivateItem(new SearchOfferViewModel());
		}

		public void ShowSettings()
		{
			ActivateItem(new SettingsViewModel());
		}

		public void ShowOrderLines()
		{
			ActivateItem(new OrderLinesViewModel());
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

#if DEBUG
		public void Snoop()
		{
			var assembly = Assembly.Load("snoop");
			var type = assembly.GetType("Snoop.SnoopUI");
			type.GetMethod("GoBabyGo", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		}
#endif

		public ShellViewModel(SerializationInfo info, StreamingContext context)
		{
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("ActiveItem", ActiveItem);
			info.AddValue("navigationChain", navigationChain);
		}

		public void Navigate(params IScreen[] views)
		{
			CancelNavigation();
			if (ActiveItem != null)
				ActiveItem.TryClose();
			var navigationChain = views.TakeWhile((s, i) => i < views.Length - 2);
			foreach (var screen in navigationChain) {
				PushInChain(screen);
			}
			ActivateItem(views.Last());
		}
	}
}