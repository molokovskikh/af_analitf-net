using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class Navigator : IDisposable
	{
		private ConductorBaseWithActiveItem<IScreen> conductor;
		private Stack<IScreen> navigationStack = new Stack<IScreen>();

		public IScreen DefaultScreen;

		public Navigator(Conductor<IScreen> conductor)
		{
			this.conductor = conductor;
		}

		public IEnumerable<IScreen> NavigationStack
		{
			get { return navigationStack; }
		}

		public void Navigate(IScreen item)
		{
			HideDefault();

			if (conductor.ActiveItem != null) {
				navigationStack.Push(conductor.ActiveItem);
				conductor.DeactivateItem(conductor.ActiveItem, false);
			}

			conductor.ActivateItem(item);
		}

		private void HideDefault()
		{
			if (conductor.ActiveItem != null && conductor.ActiveItem == DefaultScreen)
				conductor.DeactivateItem(conductor.ActiveItem, false);
		}

		public void ResetNavigation()
		{
			while (navigationStack.Count > 0) {
				var screen = navigationStack.Pop();
				CloseAndDispose(screen);
			}

			if (conductor.ActiveItem != null && conductor.ActiveItem != DefaultScreen)
				conductor.ActiveItem.TryClose();
			if (conductor.ActiveItem == null)
				conductor.ActiveItem = DefaultScreen;
		}

		public void NavigateAndReset(params IScreen[] views)
		{
			if (views.Length == 0)
				return;

			ResetNavigation();
			HideDefault();

			var chain = views.TakeWhile((s, i) => i < views.Length - 1);
			foreach (var screen in chain) {
				navigationStack.Push(screen);
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

			HideDefault();

			while (navigationStack.Count > 0) {
				var closing = navigationStack.Pop();
				if (closing.GetType() == screen.GetType()) {
					if (!ReferenceEquals(screen, closing))
						CloseAndDispose(screen);
					screen = closing;
					break;
				}
				CloseAndDispose(closing);
			}

			HideDefault();
			conductor.ActiveItem?.TryClose();
			HideDefault();

			if (IsEmptyOrDefault())
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

		private bool IsEmptyOrDefault()
		{
			return conductor.ActiveItem == null;
		}

		public void NavigateBack()
		{
			if (navigationStack.Count > 0)
				conductor.ActivateItem(navigationStack.Pop());
			else if (DefaultScreen != null)
				conductor.ActiveItem = DefaultScreen;
		}

		public void Dispose()
		{
			foreach (var screen in NavigationStack.OfType<IDisposable>())
				screen.Dispose();
			navigationStack.Clear();
		}
	}
}