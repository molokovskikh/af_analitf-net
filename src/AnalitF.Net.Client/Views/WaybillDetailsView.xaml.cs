using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Views
{
	public partial class WaybillDetailsView : UserControl
	{
		public WaybillDetailsView()
		{
			InitializeComponent();

			var grid = Lines;
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Цена производителя без НДС");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Цена ГР");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Торговая наценка оптовика");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Цена поставщика без НДС");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Цена поставщика с НДС");
			DataGridHelper.CalculateColumnWidth(grid, "000", "НДС");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Макс. розничная наценка");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Розничная наценка");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Реальная наценка");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Розничная цена");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Заказ");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Розничная сумма");

			var type = typeof(WaybillLine);
			var resources = Application.Current.Resources;
			foreach (var dataGridColumn in grid.Columns.OfType<DataGridBoundColumn>()) {
				var binding = dataGridColumn.Binding as Binding;
				if (binding == null)
					continue;
				var resource = resources[type.Name + binding.Path.Path + "Cell"] as Style;
				if (resource == null)
					continue;
				dataGridColumn.CellStyle = resource;
			}

			StyleHelper.Apply(type, grid, resources);

			Legend.Children.Add(new Label { Content = "Подсказка" });
			var styles = from p in type.GetProperties()
				from a in p.GetCustomAttributes(typeof(StyleAttribute), true)
				let key = StyleHelper.LegendKey(p)
				let style = resources[key] as Style
				where style != null
				select new Label{ Style = style };
			var stack = new StackPanel();
			stack.Orientation = Orientation.Horizontal;
			stack.Children.AddRange(styles);
			Legend.Children.Add(stack);

			var column = grid.Columns.First(c => c.Header.Equals("Розничная наценка"));
			grid.TextInput += (sender, args) => {
				if (grid.SelectedItem == null)
					return;
				var isDigit = args.Text.All(char.IsDigit);
				if (!isDigit)
					return;
				grid.CurrentCell = new DataGridCellInfo(grid.SelectedItem, column);
				grid.BeginEdit(args);
			};
		}
	}
}
