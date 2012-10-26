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
	public interface IPrintable
	{
		bool CanPrint { get; }

		void Print();
	}

	public interface IExportable
	{
		bool CanExport { get; }

		IResult Export();
	}

	[Serializable]
	public class ShellViewModel : Conductor<IScreen>
	{
		private Stack<IScreen> navigationStack = new Stack<IScreen>();

		public ShellViewModel()
		{
			AppBootstrapper.Shell = this;
			DisplayName = "АналитФАРМАЦИЯ";
			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => UpdateDisplayName());

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => RaisePropertyChangedEventImmediately("CanPrint"));

			this.ObservableForProperty(m => m.ActiveItem)
				.Subscribe(_ => RaisePropertyChangedEventImmediately("CanExport"));
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

		public bool CanExport
		{
			get
			{
				var exportable = ActiveItem as IExportable;
				if (exportable != null) {
					return exportable.CanExport;
				}
				return false;
			}
		}

		public IResult Export()
		{
			if (!CanExport)
				return null;

			return ((IExportable)ActiveItem).Export();
		}

		public bool CanPrint
		{
			get
			{
				var printable = ActiveItem as IPrintable;
				if (printable != null) {
					return printable.CanPrint;
				}
				return false;
			}
		}

		public void Print()
		{
			if (!CanPrint)
				return;

			((IPrintable)ActiveItem).Print();
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

		public void ShowJunkOffers()
		{
			ActivateItem(new JunkOfferViewModel());
		}

		public void ShowOrders()
		{
			ActivateItem(new OrdersViewModel());
		}

		public void Navigate(IScreen item)
		{
			if (ActiveItem != null) {
				navigationStack.Push(ActiveItem);
				DeactivateItem(ActiveItem, false);
			}

			ActivateItem(item);
		}

		public IEnumerable<IScreen> NavigationStack
		{
			get { return navigationStack; }
		}

		private void ResetNavigation()
		{
			while (navigationStack.Count > 0) {
				var screen = navigationStack.Pop();
				screen.TryClose();
			}
		}

		public void NavigateAndReset(params IScreen[] views)
		{
			ResetNavigation();
			if (ActiveItem != null)
				ActiveItem.TryClose();
			var chain = views.TakeWhile((s, i) => i < views.Length - 2);
			foreach (var screen in chain) {
				navigationStack.Push(screen);
			}
			ActivateItem(views.Last());
		}

		public override void DeactivateItem(IScreen item, bool close)
		{
			base.DeactivateItem(item, close);

			if (ActiveItem == null && navigationStack.Count > 0) {
				ActivateItem(navigationStack.Peek());
			}
		}

#if DEBUG
		public void Snoop()
		{
			var assembly = Assembly.Load("snoop");
			var type = assembly.GetType("Snoop.SnoopUI");
			type.GetMethod("GoBabyGo", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		}
#endif
	}
}