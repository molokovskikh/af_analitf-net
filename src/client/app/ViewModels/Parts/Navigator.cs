using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public interface INavigator
	{
		IEnumerable<IScreen> NavigationStack { get; }
		void Navigate(IScreen item);
		void NavigateRoot(IScreen screen);
		void NavigateBack();
	}

	public class Navigator : INavigator
	{
		private Conductor<IScreen>.Collection.OneActive conductor;

		public IScreen DefaultScreen;

		public Navigator(Conductor<IScreen>.Collection.OneActive conductor)
		{
			this.conductor = conductor;
		}

		public IEnumerable<IScreen> NavigationStack => conductor.Items;

		public void Navigate(IScreen item)
		{
			conductor.ActivateItem(item);
		}

		public void ResetNavigation()
		{
			foreach (var item in conductor.Items.Skip(1).Reverse()) {
				item.TryClose();
			}
		}

		public void NavigateAndReset(params IScreen[] views)
		{
			ResetNavigation();
			if (views.Length == 0)
				return;

			var chain = views.TakeWhile((s, i) => i < views.Length - 1);
			foreach (var screen in chain) {
				conductor.Items.Add(screen);
			}
			conductor.ActivateItem(views.Last());
		}

		public void Activate()
		{
			NavigateRoot(DefaultScreen);
		}

		public void NavigateRoot(IScreen screen)
		{
			if (screen == null)
				return;

			if (ReferenceEquals(screen, conductor.ActiveItem))
				return;

			if (conductor.ActiveItem != null && conductor.ActiveItem.GetType() == screen.GetType()) {
				CloseAndDispose(screen);
				return;
			}

			foreach (var item in conductor.Items.Skip(1).Reverse()) {
				if (item.GetType() == screen.GetType()) {
					if (!ReferenceEquals(screen, item))
						CloseAndDispose(screen);
					screen = item;
					break;
				}
				item.TryClose();
			}

			conductor.ActivateItem(screen);
		}

		//если форма не была инициализирована то она и не будет закрыта
		//надо явно освободить ресурсы
		//если удалить открытие сессии из конструктора basescreen то этот код не будет нужен
		public static void CloseAndDispose(IScreen screen)
		{
			screen.TryClose();
			if (screen is IDisposable) {
				((IDisposable)screen).Dispose();
			}
		}

		public void NavigateBack()
		{
			if (conductor.Items.Count > 1) {
				conductor.DeactivateItem(conductor.ActiveItem, true);
			}
		}
	}
}