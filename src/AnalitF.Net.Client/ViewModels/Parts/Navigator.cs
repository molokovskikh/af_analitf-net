using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class Navigator
	{
		private ConductorBaseWithActiveItem<IScreen> conductor;
		private Screen defaultScreen;
		private Stack<IScreen> navigationStack = new Stack<IScreen>();

		public Navigator(ConductorBaseWithActiveItem<IScreen> conductor, Screen defaultScreen)
		{
			this.defaultScreen = defaultScreen;
			this.conductor = conductor;
		}

		public IEnumerable<IScreen> NavigationStack
		{
			get { return navigationStack; }
		}

		public void Navigate(IScreen item)
		{
			if (!IsEmptyOrDefault()) {
				navigationStack.Push(conductor.ActiveItem);
				conductor.DeactivateItem(conductor.ActiveItem, false);
			}

			conductor.ActivateItem(item);
		}

		public void ResetNavigation()
		{
			while (navigationStack.Count > 0) {
				var screen = navigationStack.Pop();
				screen.TryClose();
			}

			if (conductor.ActiveItem != null && conductor.ActiveItem != defaultScreen)
				defaultScreen.TryClose();
		}

		public void NavigateAndReset(params IScreen[] views)
		{
			ResetNavigation();

			var chain = views.TakeWhile((s, i) => i < views.Length - 1);
			foreach (var screen in chain) {
				navigationStack.Push(screen);
			}
			conductor.ActivateItem(views.Last());
		}

		public void Activate()
		{
			NavigateRoot(defaultScreen);
		}

		public void NavigateRoot(IScreen screen)
		{
			if (conductor.ActiveItem != null && conductor.ActiveItem.GetType() == screen.GetType())
				return;

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
			return conductor.ActiveItem == null || conductor.ActiveItem == defaultScreen;
		}

		public void DeactivateItem(IScreen item, bool close, Action<IScreen, bool> @base)
		{
			if (item == defaultScreen)
				close = false;

			@base(item, close);

			if (close) {
				if (conductor.ActiveItem == null) {
					if (navigationStack.Count > 0)
						conductor.ActivateItem(navigationStack.Pop());
					else
						conductor.ActivateItem(defaultScreen);
				}
			}

		}
	}
}