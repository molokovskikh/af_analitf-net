using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;

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
		}
	}
}
