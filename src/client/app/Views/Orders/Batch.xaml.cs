using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class Batch : UserControl
	{
		public Batch()
		{
			InitializeComponent();

			ReportLines.CanUserSelectMultipleItems = false;
			DataGridHelper.CalculateColumnWidth(ReportLines, "Заказано", "Нет");
			DataGridHelper.CalculateColumnWidth(ReportLines, "Есть производитель", "Нет");
			DataGridHelper.CalculateColumnWidth(ReportLines, "Цена поставщика", "0000.00");
			DataGridHelper.CalculateColumnWidth(ReportLines, "Цена", "0000.00");
			DataGridHelper.CalculateColumnWidth(ReportLines, "Заказ", "0000.00");
			DataGridHelper.CalculateColumnWidth(ReportLines, "Сумма", "0000.00");
			StyleHelper.ApplyStyles(typeof(BatchLine), ReportLines, Application.Current.Resources, Legend);

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);

			DataGridHelper.CalculateColumnWidth(HistoryOrders, "00.00.0000", "Срок годн.");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000", "Заказ");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000.00", "Цена производителя");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000.00", "Цена");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "00.00.0000", "Дата");

			ReportLines.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
		}
	}
}