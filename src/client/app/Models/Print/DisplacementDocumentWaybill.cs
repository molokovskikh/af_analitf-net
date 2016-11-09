using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Models.Inventory;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	class DisplacementDocumentWaybill : BaseDocument
	{
		private DisplacementDoc d;
		private IList<DisplacementLine> lines;
		private WaybillSettings waybillSettings;

		public DisplacementDocumentWaybill(DisplacementDoc d, IList<DisplacementLine> lines, WaybillSettings waybillSettings)
		{
			this.d = d;
			this.lines = lines;
			this.waybillSettings = waybillSettings;
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			BlockStyle = new Style(typeof(Paragraph)) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
					new Setter(System.Windows.Documents.Block.MarginProperty, new Thickness(0, 3, 0, 3))
				}
			};

			HeaderStyle = new Style(typeof(Run), HeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 12d),
				}
			};
			TableHeaderStyle = new Style(typeof(TableCell), TableHeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
				}
			};
		}

		protected override void BuildDoc()
		{
			Block(new List<Grid>
			{
				Text("Организация: "),
				Text(waybillSettings.FullName),
			});
			Block(new List<Grid>
			{
				Text("Грузоотправитель: "),
				Text(d.Address.Org),
			});
			Block(new List<Grid>
			{
				Text("Адрес грузоотправителя: "),
				Text(d.Address.Name),
			});
			Block(new List<Grid>
			{
				Text("Грузополучатель: "),
				Text(d.DstAddress.Org),
			});
			Block(new List<Grid>
			{
				Text("Адрес грузополучателя: "),
				Text(d.DstAddress.Name),
			});
			Block(new List<Grid>
			{
				Text("Примечание: "),
			});


			var columns = new[] {
				new PrintColumn("№ п/п", 170),
				new PrintColumn("Наименование/Производитель, внутренний штрихкод", 80),
				new PrintColumn("Кол-во", 80),
				new PrintColumn("Цена с НДС", 80),
				new PrintColumn("Сумма", 80),
				new PrintColumn("Серия/Срок годности", 80),
				new PrintColumn("Рег. номер.", 80),
				new PrintColumn("Сертификат", 80),
			};
			var rows = lines.Select((l, i) => new object[] {
				i+1,
				l.Product + " " + l.Producer + " " + l.SerialNumber,
				l.Quantity,
				l.SupplierCost,
				l.SupplierSum,
				l.SerialNumber,
				null,
				l.Certificates,
			});
			var dataTable = BuildTable(rows, columns);

			var supplierSum = lines.Sum(l => l.SupplierSum);
			var result = new TableRow();
			result.FontWeight = FontWeights.Bold;
			result.Cells.Add(Cell("Итого", 4));
			result.Cells.Add(Cell(supplierSum));
			dataTable.RowGroups[0].Rows.Add(result);
			doc.Blocks.Add(dataTable);

			Block(new List<Grid>
			{
				Text("Сумма документа: "),
			});
			Block(new List<Grid>
			{
				Text("Отпустил:____________________________"),
			});
			Block(new List<Grid>
			{
				Text("Получил:____________________________"),
			});
		}

		private BlockUIContainer Block(List<Grid> items)
		{
			var bodyBlock = new BlockUIContainer();
			bodyBlock.Child = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0, 0, 0, 0),
				Width = 1069,
			};
			doc.Blocks.Add(bodyBlock);
			var grid = (Grid)bodyBlock.Child;
			var column = 0;
			foreach (var item in items)
			{
				grid.Cell(0, column, item);
				grid.ColumnDefinitions[column].Width = GridLength.Auto;
				column++;
			}
			grid.ColumnDefinitions[column - 1].Width = new GridLength(1, GridUnitType.Star);
			return bodyBlock;
		}
		private Grid Text(string text)
		{
			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition()
				}
			};
			grid
				.Cell(0, 0, new Label
				{
					Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 10,
					Text = text,
					TextWrapping = TextWrapping.Wrap
				}
				});
			return grid;
		}

		private Grid TextWithLine(string text)
		{
			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition()
				}
			};
			grid
				.Cell(0, 0, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 10,
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(0, 0, 0, 1),
					SnapsToDevicePixels = true,
					Content = text,
				});
			return grid;
		}
	}
}
