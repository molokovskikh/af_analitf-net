using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class Batch2 : UserControl
	{
		public Batch2()
		{
			InitializeComponent();
			Loaded += (sender, args) => {
				ApplyStyles();
			};
			Editable.AutoEditOnDigit(BatchLines, "Количество");
			new Editable().Attach(OrderLines);
			DataGridHelper.CalculateColumnWidths(OrderLines);
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "00.00.0000", "Срок годн.");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000", "Заказ");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000.00", "Цена производителя");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000.00", "Цена");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "00.00.0000", "Дата");

			BatchLines.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(BatchLine), BatchLines, Application.Current.Resources, Legend);
			var elements = Legend.Descendants<FrameworkElement>().Where(e => Equals(e.Tag, "generated")).ToArray();
			elements.Each(e => e.Tag = "");
			StyleHelper.ApplyStyles(typeof(OrderLine), OrderLines, Application.Current.Resources, Legend);
			elements.Each(e => e.Tag = "generated");
		}
	}
}
