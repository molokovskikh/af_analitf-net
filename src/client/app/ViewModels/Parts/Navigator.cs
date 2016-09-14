using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class Navigator
	{
		private Conductor<IScreen>.Collection.OneActive conductor;
		private List<IScreen> navigationStack = new List<IScreen>();

		public Navigator(Conductor<IScreen>.Collection.OneActive conductor)
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
			CloseActive();
			if (navigationStack.Count > 0) {
				var screen = navigationStack.Last();
				conductor.ActivateItem(screen);
				navigationStack.Remove(screen);
			}
		}

		public void CloseActive()
		{
			CloseScreen(conductor.ActiveItem);
		}

		public void CloseScreen(IScreen item)
		{
			if (item == null)
				return;

			//исправление для ошибки
			//Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.TabControl', AncestorLevel='1''. BindingExpression:Path=TabStripPlacement; DataItem=null; target element is 'TabItem' (Name=''); target property is 'NoTarget' (type 'Object')
			var view = (FrameworkElement)conductor.GetView();
			if (view != null)
			{
				var tabs = view.Descendants<TabControl>().First(x => x.Name == "Items");
				var tab = (TabItem)tabs.ItemContainerGenerator.ContainerFromItem(item);
				if (tab != null)
					tab.Template = null;
			}

			conductor.Items.Remove(item);
			conductor.DeactivateItem(item, true);
			navigationStack.Remove(item);
			(item as IDisposable)?.Dispose();
		}
	}
}