using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.UI;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Views
{
	public partial class WaybillDetailsView : UserControl
	{
		public WaybillDetailsView()
		{
			InitializeComponent();

			//в xaml назначение имени для колонки не работает
			CertificateLink.SetValue(NameProperty, "CertificateLink");

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

			ApplyStyles();
			//очередная магия wpf
			//
			//datagridcolumn живет вне logica|visual tree
			//и по этому стандартный биндинг не работает
			//внутри биндинга есть проверки которые пытаются найти родителя
			//для колонки родитель null и биндинг отказывается работать
			//самое смешное что при поиске родителя даже есть специальный финт для таких элементов которые находятся вне дерева
			//но перегрузка internal свойства который возвращает контекст в DataGridColumn не реализована
			//проверка родителя не работает в двух случаях если source задан явно и если он задан через relative source
			//relative source работать не будет тк колонка вне дерева
			//source нельзя задать в xaml тк он обнаруживает какую то рекурсию, с этим говном я уже не стал разбираться
			//
			//Loaded используется тк при конструировании он получит родительский datacontext а он будет shellviewmodel
			//и будет ошибка по этому ждем как все загрузится будет установлен правильный контекст и после этого создаем биндинг
			Loaded += (sender, args) => {
				foreach (var gridColumn in Lines.Columns.OfType<DataGridTextColumn>().Where(c => !c.IsReadOnly)) {
					BindingOperations.SetBinding(gridColumn, DataGridColumn.IsReadOnlyProperty,
						new Binding("DataContext.Waybill.IsReadOnly") {
							Source = grid
						});
				}

				if (DataContext != null && !((WaybillDetails)DataContext).Waybill.IsCreatedByUser) {
					grid.Columns.Remove(DataGridHelper.GetColumn(grid.Columns, "ЖНВЛС"));
				}
			};

			var column = DataGridHelper.GetColumn(grid.Columns, "Розничная наценка");
			grid.TextInput += (sender, args) => {
				if (grid.SelectedItem == null)
					return;
				var isDigit = args.Text.All(char.IsDigit);
				if (!isDigit)
					return;
				grid.CurrentCell = new DataGridCellInfo(grid.SelectedItem, column);
				grid.BeginEdit(args);
			};

			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Цена");
			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Заказ");
			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Сумма");
			//тк мы не можем получить информацию о типе от view model
			//задаем ее статично
			Conventions.ConfigureDataGrid(Lines, typeof(WaybillLine));
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(WaybillLine), Lines, Application.Current.Resources, Legend);
		}
	}
}
