using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class Navigator
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
				screen.TryClose();
			}

			if (conductor.ActiveItem != null && conductor.ActiveItem != DefaultScreen)
				conductor.ActiveItem.TryClose();
			if (conductor.ActiveItem == null)
				conductor.ActiveItem = DefaultScreen;
		}

		public void NavigateAndReset(params IScreen[] views)
		{
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
			if (conductor.ActiveItem != null && conductor.ActiveItem.GetType() == screen.GetType())
				return;

			HideDefault();

			while (navigationStack.Count > 0) {
				var closing = navigationStack.Peek();
				if (closing.GetType() == screen.GetType())
					break;
				navigationStack.Pop();
				closing.TryClose();
			}

			if (conductor.ActiveItem != null)
				conductor.ActiveItem.TryClose();

			if (IsEmptyOrDefault())
				conductor.ActivateItem(screen);
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
	}
}