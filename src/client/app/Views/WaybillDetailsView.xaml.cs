﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;
using System.Windows.Threading;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using log4net.Repository.Hierarchy;
using NHibernate.Linq;

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
		private bool isBarcode;
		private StringBuilder code = new StringBuilder();

		public WaybillDetailsView(WaybillDetails model)
		{
			this.model = model;
			Init();
		}

		public WaybillDetailsView()
		{
			Init();
		}

		protected bool KeyboardInput(string key)
		{
			if (string.IsNullOrEmpty(key))
				return false;
			var model = (WaybillDetails)DataContext;
			var settings = model.Settings.Value;
			if (!isBarcode) {
				if (key[0] == settings.BarCodePrefix) {
					isBarcode = true;
					return true;
				}
			} else if (key[0] == settings.BarCodeSufix) {
				model.HandleBarCode(code.ToString());
				code.Clear();
				isBarcode = false;
				return true;
			} else {
				code.Append(key);
				return true;
			}
			return false;
		}

		private void Init()
		{
			InitializeComponent();

			PreviewKeyDown += (sender, args) => {
				args.Handled = KeyboardInput(KeyboardHook.KeyToUnicode(args.Key));
			};

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
				SortDirection = ListSortDirection.Ascending
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Производитель",
				Binding = new Binding("Producer"),
				Width = new DataGridLength(180, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Страна",
				Binding = new Binding("Country"),
				Width = new DataGridLength(100, DataGridLengthUnitType.Star),
			});
			lines.Columns.Add(CheckBoxColumn("Печатать", "Print",
				x => lines.Items.OfType<WaybillLine>().Each(l => l.Print = x), true));
			var column = CheckBoxColumn("Оприходовать", "IsReadyForStock",
				x => {
					lines.Items.OfType<WaybillLine>().Each(l => {
						l.Receive(x ? l.Quantity.GetValueOrDefault() : 0);
					});

				}, false);
			lines.Columns.Add(column);
			lines.Columns.Add(new DataGridTextColumnEx {
				Header = "Оприходовано",
				Binding = new Binding("ReceivedQuantity"),
				SortMemberPath = "ReceivedQuantity",
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
			});
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
				Visibility = Visibility.Collapsed
			});
			lines.Columns.Add(new CustomDataGridColumn((c, i) => null) {
				Header = "Сертификаты",
				Name = "CertificateLink",
				Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader),
				Generator = (c, i) => new ContentControl { Style = (Style)FindResource("DownloadLink") }
			});
			if (model?.Waybill.IsCreatedByUser == true) {
				lines.CanUserAddRows = true;
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
				Binding = new Binding("SupplierCost"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
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
				Binding = new Binding("Quantity"),
				Width = new DataGridLength(1, DataGridLengthUnitType.Star),
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
			};

			MainGrid.Children.Add(lines);

			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Цена");
			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Заказ");
			DataGridHelper.CalculateColumnWidth(OrderLines, "00000.00", "Сумма");
		}

		private DataGridColumn CheckBoxColumn(string header, string member, Action<bool> checkAll, bool isChecked)
		{
			var column = new CustomDataGridColumn {
				Header = CheckAllHeader(header, checkAll, isChecked),
				Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader),
				SortMemberPath = member,
				Generator = (c, i) => {
					var el = new CheckBox {
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Center
					};
					BindingOperations.SetBinding(el, CheckBox.IsCheckedProperty, new Binding(member) {
						UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
					});
					return el;
				}
			};
			DataGridHelper.SetColumnDisplayName(column, header);
			return column;
		}

		private StackPanel CheckAllHeader(string name, Action<bool> checkAll, bool isChecked)
		{
			var check = new CheckBox {
				IsChecked = isChecked,
				Focusable = false,
				VerticalAlignment = VerticalAlignment.Center,
				Name = "CheckAllPrint"
			};
			var textBlock = new TextBlock {
				Text = name
			};
			var header = new StackPanel {
				Orientation = Orientation.Horizontal,
				Children = {check, textBlock}
			};
			check.Checked += (sender, args) => checkAll(check.IsChecked.GetValueOrDefault());
			check.Unchecked += (sender, args) => checkAll(check.IsChecked.GetValueOrDefault());
			return header;
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(WaybillLine), lines, Application.Current.Resources, Legend);
		}
	}
}
