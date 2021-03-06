﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class Batch : UserControl
	{
		public Batch()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
			};

			DataGridHelper.CalculateColumnWidths(ReportLines);
			DataGridHelper.CalculateColumnWidth(ReportLines, "Нет", "Заказано");
			DataGridHelper.CalculateColumnWidth(ReportLines, "Нет", "Есть производитель");
			DataGridHelper.CalculateColumnWidth(ReportLines, "0000.00", "Цена поставщика");
			DataGridHelper.CalculateColumnWidth(ReportLines, "0000.00", "Цена");
			DataGridHelper.CalculateColumnWidth(ReportLines, "0000.00", "Количество");
			DataGridHelper.CalculateColumnWidth(ReportLines, "0000.00", "Заказ");
			DataGridHelper.CalculateColumnWidth(ReportLines, "0000.00", "Сумма");
			DataGridHelper.CalculateColumnWidth(ReportLines, "0000000000000", "Штрихкод");

			new Editable().Attach(Offers);
			DataGridHelper.CalculateColumnWidths(Offers);
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "00.00.0000", "Срок годн.");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000", "Заказ");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000.00", "Цена производителя");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "000.00", "Цена");
			DataGridHelper.CalculateColumnWidth(HistoryOrders, "00.00.0000", "Дата");

			ReportLines.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
			new Editable().Attach(ReportLines);
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(BatchLineView), ReportLines, Application.Current.Resources, Legend);
			var elements = Legend.Descendants<FrameworkElement>().Where(e => Equals(e.Tag, "generated")).ToArray();
			elements.Each(e => e.Tag = "");
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
			elements.Each(e => e.Tag = "generated");
		}
	}
}