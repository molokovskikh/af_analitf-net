using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class TabNavigator : INavigator
	{
		private Conductor<IScreen>.Collection.OneActive conductor;
		private List<IScreen> navigationStack = new List<IScreen>();

		public TabNavigator(Conductor<IScreen>.Collection.OneActive conductor)
		{
			this.conductor = conductor;
		}

		public IEnumerable<IScreen> NavigationStack => navigationStack;

		public void Navigate(IScreen item)
		{
			if (conductor.ActiveItem != null)
				navigationStack.Add(conductor.ActiveItem);
			conductor.ActivateItem(item);
		}

		public void NavigateRoot(IScreen screen)
		{
			navigationStack.Clear();
			var same = conductor.Items.FirstOrDefault(x => x.GetType() == screen.GetType());
			if (same != null) {
				(screen as IDisposable)?.Dispose();
				conductor.ActivateItem(same);
			} else {
				conductor.ActivateItem(screen);
			}
		}

		public void NavigateBack()
		{
			CloseScreen(conductor.ActiveItem);
			if (navigationStack.Count > 0) {
				var screen = navigationStack.Last();
				conductor.ActivateItem(screen);
				navigationStack.Remove(screen);
			}
		}

		public void CloseScreen(IScreen item)
		{
			if (item == null)
				return;

			conductor.DeactivateItem(item, true);
			conductor.Items.Remove(item);
			Release(item);
		}

		public void Release(IScreen item)
		{
			navigationStack.Remove(item);
			(item as IDisposable)?.Dispose();
		}

		public void CloseAll()
		{
			for (var i = conductor.Items.Count - 1; i >= 0; i--)
				CloseScreen(conductor.Items[i]);
		}
	}
}