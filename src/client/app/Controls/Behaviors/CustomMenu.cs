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
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;

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
				MarkAsReadOrUnread(AssociatedObject, false).LogResult();
			};
			AssociatedObject.ContextMenu.Items.Insert(0, menuItem);
			menuItem = new MenuItem() {Header = "Пометить как прочтённые"};
			menuItem.Click += (sender, args) =>
			{
				args.Handled = true;
				MarkAsReadOrUnread(AssociatedObject, true).LogResult();
			};
			AssociatedObject.ContextMenu.Items.Insert(0, menuItem);
		}

		private async Task MarkAsReadOrUnread(DataGrid grid, bool read)
		{
			IList<Waybill> waybills = new List<Waybill>();
			foreach (var wb in grid.SelectedItems)
			{
				waybills.Add(wb as Waybill);
			}
			if (waybills.Count == 0 && grid.CurrentItem != null)
			{
				waybills.Add(grid.CurrentItem as Waybill);
			}
			if (waybills.Count == 0) return;

			var context = grid.DataContext as BaseScreen;
			await context.Env.Query(s => UpdateWaybills(context, waybills, !read, s));

			grid.Items.Refresh();
		}

		private void UpdateWaybills(BaseScreen context, IList<Waybill> waybills, bool newState, IStatelessSession session)
		{
			foreach (var waybill in waybills)
			{
				waybill.IsNew = newState;
				session.CreateSQLQuery("update waybills set IsNew = :isNew where Id = :id")
					.SetParameter("isNew", newState ? 1 : 0)
					.SetParameter("id", waybill.Id)
					.ExecuteUpdate();
			}
			context.Env.RxQuery(x => x.Query<Waybill>().Count(r => r.IsNew)).Subscribe(context.Shell.NewDocsCount);
		}
	}
}
