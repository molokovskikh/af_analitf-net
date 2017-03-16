using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using ReactiveUI;
using AnalitF.Net.Client.Controls;
using WinForm = System.Windows.Forms;


namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class WinFormDataGridConfig : Screen
	{
		private WinFormDataGrid grid;

		public WinFormDataGridConfig(WinFormDataGrid grid)
		{
			this.grid = grid;
			DisplayName = "Столбцы";
			List<DataGridViewTextBoxColumnEx> tmp = new List<DataGridViewTextBoxColumnEx>();
			Columns = new NotifyValue<List<DataGridViewTextBoxColumnEx>>();
			foreach (WinForm.DataGridViewColumn c in grid.DataGrid.Grid.Columns)
			{
				if (c is DataGridViewTextBoxColumnEx)
					tmp.Add((DataGridViewTextBoxColumnEx)c);
			}
			Columns.Value = tmp;

			CurrentColumn = new NotifyValue<DataGridViewTextBoxColumnEx>();
			CanHide = this.ObservableForProperty(c => c.CurrentColumn.Value.BindVisible)
				.Select(v => v.Value)
				.ToValue();
			CanShow = this.ObservableForProperty(c => c.CurrentColumn.Value.BindVisible)
				.Select(v => !v.Value)
				.ToValue();
			CanUp = this.ObservableForProperty(c => c.CurrentColumn.Value.BindDisplayIndex)
				.Select(v => v.Value > 0)
				.ToValue();
			CanDown = this.ObservableForProperty(c => c.CurrentColumn.Value.BindDisplayIndex)
				.Select(v => v.Value < Columns.Value.Count - 1)
				.ToValue();
			Columns.Value = Columns.Value.OrderBy(c => c.BindDisplayIndex).ToList();
		}

		public NotifyValue<List<DataGridViewTextBoxColumnEx>> Columns { get; set; }
		public NotifyValue<DataGridViewTextBoxColumnEx> CurrentColumn { get; set; }
		public NotifyValue<bool> CanHide { get; set; }
		public NotifyValue<bool> CanShow { get; set; }
		public NotifyValue<bool> CanUp { get; set; }
		public NotifyValue<bool> CanDown { get; set; }

		public void Up()
		{
			if (!CanUp.Value)
				return;
			CurrentColumn.Value.BindDisplayIndex--;
			Columns.Value = Columns.Value.OrderBy(c => c.BindDisplayIndex).ToList();
		}

		public void Down()
		{
			if (!CanDown.Value)
				return;
			CurrentColumn.Value.BindDisplayIndex++;
			Columns.Value = Columns.Value.OrderBy(c => c.BindDisplayIndex).ToList();
		}

		public void Hide()
		{
			if (!CanHide.Value)
				return;
			CurrentColumn.Value.BindVisible = false;
		}

		public void Show()
		{
			if (!CanShow.Value)
				return;
			CurrentColumn.Value.BindVisible = true;
		}

		public void OK()
		{
			TryClose();
		}
	}
}
