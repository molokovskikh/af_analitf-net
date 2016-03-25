using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class Navigator : IDisposable
	{
		private Conductor<IScreen>.Collection.OneActive conductor;
		private Stack<IScreen> navigationStack = new Stack<IScreen>();

		public IScreen DefaultScreen;

		public Navigator(Conductor<IScreen>.Collection.OneActive conductor)
		{
			this.conductor = conductor;
		}

		public IEnumerable<IScreen> NavigationStack => navigationStack;

		public void Navigate(IScreen item)
		{
			//HideDefault();

			//if (conductor.ActiveItem != null) {
			//	navigationStack.Push(conductor.ActiveItem);
			//	conductor.DeactivateItem(conductor.ActiveItem, false);
			//}
			if (conductor.ActiveItem != null)
				navigationStack.Push(conductor.ActiveItem);
			conductor.ActivateItem(item);
		}

		public void ResetNavigation()
		{
			navigationStack.Clear();
			//NavigationStack.cle
			//while (navigationStack.Count > 0) {
			//	var screen = navigationStack.Pop();
			//	CloseAndDispose(screen);
			//}

			//if (conductor.ActiveItem != null && conductor.ActiveItem != DefaultScreen)
			//	conductor.ActiveItem.TryClose();
			//if (conductor.ActiveItem == null)
			//	conductor.ActiveItem = DefaultScreen;
		}

		public void NavigateAndReset(params IScreen[] views)
		{
			if (views.Length == 0)
				return;

			ResetNavigation();

			var chain = views.TakeWhile((s, i) => i < views.Length - 1);
			foreach (var screen in chain) {
				navigationStack.Push(screen);
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
			//if (screen == null)
			//	return;

			//conductor.Items.Add(screen);
			ResetNavigation();
			conductor.ActivateItem(screen);

			//if (ReferenceEquals(screen, conductor.ActiveItem))
			//	return;

			//if (conductor.ActiveItem != null && conductor.ActiveItem.GetType() == screen.GetType()) {
			//	CloseAndDispose(screen);
			//	return;
			//}

			//HideDefault();

			//while (navigationStack.Count > 0) {
			//	var closing = navigationStack.Pop();
			//	if (closing.GetType() == screen.GetType()) {
			//		if (!ReferenceEquals(screen, closing))
			//			CloseAndDispose(screen);
			//		screen = closing;
			//		break;
			//	}
			//	CloseAndDispose(closing);
			//}

			//HideDefault();
			//conductor.ActiveItem?.TryClose();
			//HideDefault();

			//if (IsEmptyOrDefault())
			//	conductor.ActivateItem(screen);
		}

		//если форма не была инициализирована то она и не будет закрыта
		//надо явно освободить ресурсы
		//если удалить открытие сессии из конструктора basescreen то этот код не будет нужен
		public static void CloseAndDispose(IScreen screen)
		{
			//screen.TryClose();
			//if (screen is IDisposable) {
			//	((IDisposable)screen).Dispose();
			//}
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
			//foreach (var screen in NavigationStack.OfType<IDisposable>())
			//	screen.Dispose();
			//navigationStack.Clear();
		}
	}
}