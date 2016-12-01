using AnalitF.Net.Client.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Controls.Behaviors
{
	public class CustomMenu : Persistable
	{
		protected override void OnAttached()
		{
			base.OnAttached();

			if (AssociatedObject.Name != "Waybills") return;

			AssociatedObject.ContextMenu.Items.Insert(0, new Separator());
			var menuItem = new MenuItem() { Header = "Пометить как непрочтённые" };
			menuItem.Click += (sender, args) =>
			{
				args.Handled = true;
				MarkAsReadOrUnread(AssociatedObject, false);
			};
			AssociatedObject.ContextMenu.Items.Insert(0, menuItem);
			menuItem = new MenuItem() {Header = "Пометить как прочтённые"};
			menuItem.Click += (sender, args) =>
			{
				args.Handled = true;
				MarkAsReadOrUnread(AssociatedObject, true);
			};
			AssociatedObject.ContextMenu.Items.Insert(0, menuItem);
		}

		private void MarkAsReadOrUnread(DataGrid grid, bool read)
		{
			var context = grid.DataContext as BaseScreen;
			var waybills = new List<Waybill>();
			foreach (var wb in grid.SelectedItems)
			{
				waybills.Add(wb as Waybill);
			}
			if (waybills.Count == 0 && grid.CurrentItem != null)
			{
				waybills.Add(grid.CurrentItem as Waybill);
			}
			if (waybills.Count == 0) return;

			foreach (var waybill in waybills)
			{
				waybill.IsNew = !read;
				context.Session.SaveOrUpdate(waybill);
			}
			context.Session.Flush();

			grid.Items.Refresh();
			context.Shell.NewDocsCount.Value = context.Session.Query<Waybill>().Count(r => r.IsNew);
		}
	}
}
