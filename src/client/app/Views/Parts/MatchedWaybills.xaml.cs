using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Views.Parts
{
	public partial class MatchedWaybills : UserControl
	{
		public MatchedWaybills()
		{
			InitializeComponent();

			DataGridHelper.CalculateColumnWidth(WaybillLines, "99999.99", "Цена производителя без НДС");
			DataGridHelper.CalculateColumnWidth(WaybillLines, "99999.99", "Цена ГР");
			DataGridHelper.CalculateColumnWidth(WaybillLines, "99999.99", "Торговая наценка оптовика");
			DataGridHelper.CalculateColumnWidth(WaybillLines, "99999.99", "Цена поставщика без НДС");
			DataGridHelper.CalculateColumnWidth(WaybillLines, "999", "НДС");
			DataGridHelper.CalculateColumnWidth(WaybillLines, "99999.99", "Цена поставщика с НДС");
			DataGridHelper.CalculateColumnWidth(WaybillLines, "99999.99", "Заказ");
		}
	}
}
