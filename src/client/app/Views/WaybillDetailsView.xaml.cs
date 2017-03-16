using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;

namespace AnalitF.Net.Client.Views
{
	public class CustomDataGridColumn : DataGridColumn
	{
		public Func<DataGridCell, object, FrameworkElement> Generator;

		public CustomDataGridColumn()
		{
		}

		public CustomDataGridColumn(Func<DataGridCell, object, FrameworkElement> generator)
		{
			this.Generator = generator;
		}

		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			return Generator(cell, dataItem);
		}

		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{
			return Generator(cell, dataItem);
		}

		public string Name
		{
			get { return (string)GetValue(FrameworkElement.NameProperty); }
			set { SetValue(FrameworkElement.NameProperty, value); }
		}
	}

	public partial class WaybillDetailsView : UserControl
	{
		private DataGrid2 lines;
		private WaybillDetails model;

		public WaybillDetailsView()
		{
			InitializeComponent();
			DataContextChanged += OnDataContextChanged;

			var element = Rounding;
			var items = DescriptionHelper.GetDescriptions(typeof(Rounding));
			element.ItemsSource = items;
			element.DisplayMemberPath = "Name";

			var binding = new Binding("Waybill.Rounding");
			binding.Converter = new ComboBoxSelectedItemConverter();
			binding.ConverterParameter = items;
			BindingOperations.SetBinding(element, Selector.SelectedItemProperty, binding);
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			model = DataContext as WaybillDetails;
			if (model == null)
				return;

			DataContextChanged -= OnDataContextChanged;
			model.SkipRestoreTable = true;
			Init();
		}

		private void Init()
		{
			if (model?.Waybill == null)
				return;
			//борьба за производительность
			//операции установки стиля приводят к перестроению дерева элементов wpf
			//что негативно отражается на производительности
			//что бы избежать ненужных перестроений дерева
			//сначала конструируем таблицу и настраиваем ее а затем добавляем в дерево элементов
			lines = new DataGrid2();
			lines.Loaded += (sender, args) => {
				DataGridHelper.Focus(lines);
			};
			lines.IsReadOnly = false;
			lines.Name = "Lines";
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Наименование",
				Binding = new Binding("Product"),
				Width = new DataGridLength(180, DataGridLengthUnitType.Star),
				SortDirection = ListSortDirection.Ascending,
				IsReadOnly = true
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Производитель",
				Binding = new Binding("Producer"),
				Width = new DataGridLength(180, DataGridLengthUnitType.Star),
				IsReadOnly = true
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Страна",
				Binding = new Binding("Country"),
				Width = new DataGridLength(100, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(DataGridHelper.CheckBoxColumn("Печатать", "Print",
				x => lines.Items.OfType<WaybillLine>().Each(l => l.Print = x), true));
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Срок годности",
				Binding = new Binding("Period"),
				SortMemberPath = "Exp",
				Width = new DataGridLength(160, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Серия товара",
				Binding = new Binding("SerialNumber"),
				Width = new DataGridLength(160, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Width = new DataGridLength(13, DataGridLengthUnitType.Star),
				Header = "Штрихкод",
				Binding = new Binding("EAN13"),
			});
			lines.Columns.Add(new CustomDataGridColumn((c, i) => null) {
				Header = "Сертификаты",
				Name = "CertificateLink",
				Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader),
				Generator = (c, i) => new ContentControl { Style = (Style)FindResource("DownloadLink") }
			});
			if (model.Waybill.IsCreatedByUser == true) {
				lines.CanUserDeleteRows = true;
				lines.Columns.Add(new CustomDataGridColumn {
					Header = "ЖНВЛС",
					Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader),
					Generator = (c, i) => {
						var el = new CheckBox {
							VerticalAlignment = VerticalAlignment.Center,
							HorizontalAlignment = HorizontalAlignment.Center
						};
						BindingOperations.SetBinding(el, CheckBox.IsCheckedProperty, new Binding("VitallyImportant") {
							UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
						});
						return el;
					}
				});
			}
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Номер сертификата",
				Binding = new Binding("Certificates"),
				Width = new DataGridLength(180, DataGridLengthUnitType.Star),
				Visibility = Visibility.Collapsed
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Цена производителя без НДС",
				Binding = new Binding("ProducerCost"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Цена ГР",
				Binding = new Binding("RegistryCost"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Торговая наценка оптовика",
				Binding = new Binding("SupplierPriceMarkup"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Цена поставщика без НДС",
				Binding = new Binding("SupplierCostWithoutNds"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "НДС",
				Binding = new Binding("Nds"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Цена поставщика с НДС",
				Binding = new Binding("SupplierCost") { Converter = InputConverter.Instance, ValidatesOnExceptions = true, },
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
				IsReadOnly = false
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Макс. розничная наценка",
				Binding = new Binding("MaxRetailMarkup"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
				IsReadOnly = true,
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Розничная наценка",
				Binding = new Binding("RetailMarkup") { Converter = InputConverter.Instance, ValidatesOnExceptions = true, },
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
				IsReadOnly = false,
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Реальная наценка",
				Binding = new Binding("RealRetailMarkup") { Converter = InputConverter.Instance, ValidatesOnExceptions = true, },
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
				IsReadOnly = false,
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Розничная цена",
				Binding = new Binding("RetailCost") {
					Converter = InputConverter.Instance, ValidatesOnExceptions = true,
				},
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
				IsReadOnly = false,
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Заказ",
				Binding = new Binding("Quantity") { Converter = InputConverter.Instance, ValidatesOnExceptions = true, },
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
				IsReadOnly = false
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Розничная сумма",
				Binding = new Binding("RetailSum"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
				IsReadOnly = true,
			});

			var grid = lines;
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
			DataGridHelper.CalculateColumnWidth(grid, "00000", "Оприходовано");
			DataGridHelper.CalculateColumnWidth(grid, "00000.00", "Розничная сумма");
			DataGridHelper.CalculateColumnWidth(grid, "0000000000000", "Штрихкод");

			StyleHelper.ApplyStyles(typeof(WaybillLine), lines, Application.Current.Resources, Legend);
			Conventions.ConfigureDataGrid(lines, typeof(WaybillLine));

			Grid.SetRow(lines, 2);
			Grid.SetColumn(lines, 1);
			Editable.AutoEditOnDigit(grid, "Розничная наценка");
			if (model != null) {
				model.TableSettings.Restore(lines);
				model.TableSettings.Restore(OrderLines);

				foreach (var gridColumn in lines.Columns.OfType<DataGridTextColumn>().Where(c => !c.IsReadOnly)) {
					if (gridColumn.ReadLocalValue(DataGridColumn.IsReadOnlyProperty) == DependencyProperty.UnsetValue) {
						gridColumn.IsReadOnly = model.Waybill.IsReadOnly;
					}
				}
				model.Waybill.PropertyChanged += (sender, args) => {
					if (args.PropertyName == "IsCreatedByUser") {
						MainGrid.Children.Remove(lines);
						Init();
						lines.ItemsSource = model.Lines.Value;
					}
					else if (args.PropertyName == "Status") {
						lines.IsReadOnly = model.Waybill.Status == DocStatus.Posted;
					}
				};

				lines.IsReadOnly = model.Waybill.Status == DocStatus.Posted;
			}
			grid.BeginningEdit += (sender, args) => {
				var line = args.Row.DataContext as WaybillLine;
				if (line == null)
					return;
				if (line.ServerRetailCost != null) {
					MessageBox.Show(Application.Current.MainWindow, "Редактирование розничной цены запрещено поставщиком",
						Consts.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
					args.Cancel = true;
				}
				else if (model.Waybill.Status == DocStatus.Posted) {
					MessageBox.Show(Application.Current.MainWindow, "Накладная оприходована, редактирование запрещено",
						Consts.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
					args.Cancel = true;
				}
			};

			MainGrid.Children.Add(lines);

			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Цена");
			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Заказ");
			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Сумма");
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(WaybillLine), lines, Application.Current.Resources, Legend);
		}
	}
}
