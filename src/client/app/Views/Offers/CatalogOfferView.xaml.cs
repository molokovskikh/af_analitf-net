using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using System.Windows.Input;
using AnalitF.Net.Client.ViewModels.Offers;
using WinForms = System.Windows.Forms;
using AnalitF.Net.Client.Controls;

namespace AnalitF.Net.Client.Views.Offers
{
	public partial class CatalogOfferView : UserControl
	{
		public CatalogOfferView()

		{

			InitializeComponent();
			Offers.Type = typeof(Offer);
			Offers.StyleResources = Application.Current.Resources;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "ProductSynonym", DataPropertyName = "ProductSynonym", HeaderText = "Название", WidthWPF = 177 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "ProducerSynonym", DataPropertyName = "ProducerSynonym", HeaderText = "Производитель", WidthWPF = 89 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Producer", DataPropertyName = "Producer", Visible = false, HeaderText = "Кат.производитель", WidthWPF = 33 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Volume", DataPropertyName = "Volume", HeaderText = "Упаковка", WidthWPF = 33 });
			Offers.DataGrid.Columns["Volume"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Doc", DataPropertyName = "Doc", HeaderText = "Документ", WidthWPF = 33 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Note", DataPropertyName = "Note", HeaderText = "Примечание", WidthWPF = 48 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Period", DataPropertyName = "Period", HeaderText = "Срок годн.", WidthWPF = 56 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "PriceName", DataPropertyName = "PriceName", PropertyPath = "Price.Name",HeaderText = "Прайс -лист", WidthWPF = 74 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "PriceRegionName", DataPropertyName = "PriceRegionName", Visible = false, HeaderText = "Регион", WidthWPF = 72 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "PricePriceDate", DataPropertyName = "PricePriceDate", PropertyPath = "Price.PriceDate", Visible = false, HeaderText = "Дата прайс-листа", WidthWPF = 87 });
			Offers.DataGrid.Columns["PricePriceDate"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleCenter;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "RequestRatio", DataPropertyName = "RequestRatio", Visible = false, HeaderText = "Кратность", WidthWPF = 61 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "MinOrderSum", DataPropertyName = "MinOrderSum", Visible = false, HeaderText = "Мин.сумма", WidthWPF = 40 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "MinOrderCount", DataPropertyName = "MinOrderCount", Visible = false, HeaderText = "Мин.кол-во", WidthWPF = 40 });
			Offers.DataGrid.Columns["MinOrderCount"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Diff", DataPropertyName = "Diff", Visible = false, HeaderText = "Разница, %", WidthWPF = 40 });
			Offers.DataGrid.Columns["Diff"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "RegistryCost", DataPropertyName = "RegistryCost", HeaderText = "Реестр.цена", WidthWPF = 46 });
			Offers.DataGrid.Columns["RegistryCost"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "MaxProducerCost", DataPropertyName = "MaxProducerCost", HeaderText = "Пред.зарег.цена", WidthWPF = 20 });
			Offers.DataGrid.Columns["MaxProducerCost"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "ProducerCost", DataPropertyName = "ProducerCost", HeaderText = "Цена производителя", WidthWPF = 20 });
			Offers.DataGrid.Columns["ProducerCost"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "SupplierMarkup", DataPropertyName = "SupplierMarkup", HeaderText = "Наценка поставщика", WidthWPF = 20 });
			Offers.DataGrid.Columns["SupplierMarkup"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "NDS", DataPropertyName = "NDS", HeaderText = "НДС", WidthWPF = 20 });
			Offers.DataGrid.Columns["NDS"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Cost", DataPropertyName = "Cost", HeaderText = "Цена поставщика", WidthWPF = 20 });
			Offers.DataGrid.Columns["Cost"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "ResultCost", DataPropertyName = "ResultCost", HeaderText = "Цена", WidthWPF = 39 });
			Offers.DataGrid.Columns["ResultCost"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns["ResultCost"].DefaultCellStyle.Font = new System.Drawing.Font(Offers.DataGrid.DefaultCellStyle.Font, System.Drawing.FontStyle.Bold);
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "Остаток", WidthWPF = 48 });
			Offers.DataGrid.Columns["Quantity"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "RetailCost", DataPropertyName = "RetailCost", HeaderText = "Розн.цена", WidthWPF = 48 });
			Offers.DataGrid.Columns["RetailCost"].DefaultCellStyle.Alignment = WinForms.DataGridViewContentAlignment.MiddleRight;
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "OrderLineComment", DataPropertyName = "OrderLineComment", PropertyPath = "OrderLine.Comment", HeaderText = "Комментарий", WidthWPF = 48 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "OrderCount", DataPropertyName = "OrderCount", HeaderText = "Заказ", WidthWPF = 51 });
			Offers.DataGrid.Columns.Add(new DataGridViewTextBoxColumnEx() { Name = "OrderLineResultSum", DataPropertyName = "OrderLineResultSum", PropertyPath= "OrderLine.ResultSum", HeaderText = "Сумма", WidthWPF = 51 });
			new Editable().Attach(Offers);
			ApplyStyles();
			BindingOperations.SetBinding(OfferOverlayPanel, Grid.MaxHeightProperty,
				new Binding("ActualHeight") {
					Source = Offers,
					Converter = new LambdaConverter<double>(v => v * 0.7)
				});

			var element = Rounding;
			var items = DescriptionHelper.GetDescriptions(typeof(Rounding));
			element.ItemsSource = items;
			element.DisplayMemberPath = "Name";

			var binding = new Binding("Rounding.Value") {
				Converter = new ComboBoxSelectedItemConverter(),
				ConverterParameter = items
			};
			BindingOperations.SetBinding(element, Selector.SelectedItemProperty, binding);
		}


		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(Offer), Offers, Application.Current.Resources, Legend);
			StyleHelper.ApplyStyles(typeof(SentOrderLine), HistoryOrders, Application.Current.Resources);
		}

	}
}
