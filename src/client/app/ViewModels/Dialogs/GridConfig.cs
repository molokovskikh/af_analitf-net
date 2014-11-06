using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using Common.Tools;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class GridConfig : Screen
	{
		private double factor;
		private DataGrid grid;

		public GridConfig(DataGrid grid)
		{
			var column = grid.Columns.First(c => c.Width.UnitType == DataGridLengthUnitType.Star);
			factor = column.ActualWidth / column.Width.Value;
			this.grid = grid;
			DisplayName = "Столбцы";
			Columns = new NotifyValue<List<ColumnSettings>>(grid.Columns.Select((c, i) => new ColumnSettings(grid, c, i)).OrderBy(c => c.DisplayIndex).ToList());
			CurrentColumn = new NotifyValue<ColumnSettings>();
			CanHide = this.ObservableForProperty(c => c.CurrentColumn.Value.IsVisible)
				.Select(v => v.Value)
				.ToValue();
			CanShow = this.ObservableForProperty(c => c.CurrentColumn.Value.IsVisible)
				.Select(v => !v.Value)
				.ToValue();
			CanUp = this.ObservableForProperty(c => c.CurrentColumn.Value.DisplayIndex)
				.Select(v => v.Value > 0)
				.ToValue();
			CanDown = this.ObservableForProperty(c => c.CurrentColumn.Value.DisplayIndex)
				.Select(v => v.Value < Columns.Value.Count - 1)
				.ToValue();
		}

		public NotifyValue<List<ColumnSettings>> Columns { get; set; }
		public NotifyValue<ColumnSettings> CurrentColumn { get; set; }
		public NotifyValue<bool> CanHide { get; set; }
		public NotifyValue<bool> CanShow { get; set; }
		public NotifyValue<bool> CanUp { get; set; }
		public NotifyValue<bool> CanDown { get; set; }

		public void Up()
		{
			if (!CanUp.Value)
				return;
			var prev = Columns.Value.Last(c => c.DisplayIndex < CurrentColumn.Value.DisplayIndex);
			prev.DisplayIndex++;
			CurrentColumn.Value.DisplayIndex--;
			Columns.Value = Columns.Value.OrderBy(c => c.DisplayIndex).ToList();
		}

		public void Down()
		{
			if (!CanDown.Value)
				return;
			var next = Columns.Value.First(c => c.DisplayIndex > CurrentColumn.Value.DisplayIndex);
			next.DisplayIndex--;
			CurrentColumn.Value.DisplayIndex++;
			Columns.Value = Columns.Value.OrderBy(c => c.DisplayIndex).ToList();
		}

		public void Hide()
		{
			if (!CanHide.Value)
				return;
			CurrentColumn.Value.IsVisible = false;
		}

		public void Show()
		{
			if (!CanShow.Value)
				return;
			CurrentColumn.Value.IsVisible = true;
		}

		public void OK()
		{
			Columns.Value.Where(c => c.IsDirty).Each(c => {
				c.Width = new DataGridLength(c.PixelWidth / factor, DataGridLengthUnitType.Star);
				c.Restore(grid, grid.Columns);
			});
			TryClose();
		}
	}
}