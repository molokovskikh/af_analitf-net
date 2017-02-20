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
	class PriceNegotiationProtocol : BaseDocument
	{
		private DisplacementDoc d;
		private IList<DisplacementLine> lines;
		private string fio;

		public PriceNegotiationProtocol(DisplacementDoc d, IList<DisplacementLine> lines, string fio)
		{
			this.d = d;
			this.lines = lines;
			this.fio = fio;
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
				TextWithLineSign(d.Address.Org, "поставщик"),
			});
			Block(new List<Grid>
			{
				TextWithLineSign(d.DstAddress.Org, "получатель (организация оптовой торговли или организации розничной торговли)"),
			});

			var columns = new[] {
				new PrintColumn("Торговое наименование, лекарственная форма, дозировка, количество в потребительской упаковке", 150),
				new PrintColumn("Серия", 70),
				new PrintColumn("Производитель", 70),
				new PrintColumn("Зарегистри-рованная \n" +
					"предельная отпускная цена, \n" +
					"установленная произво-\n" +
					"дителем (рублей)", 80),
				new PrintColumn("Фактическая \n" +
					"отпускная \n" +
					"цена, уста- \n" +
					"новленная \n" +
					"производи- \n" +
					"телем, \n" +
					"без НДС (рублей)", 60),
				new PrintColumn("проц.", 30),
				new PrintColumn("рублей", 30),
				new PrintColumn("Фактическаяотпускная\n" +
					"цена, уста\n-" +
					"новленнаяорганизаци-\n" +
					"ей оптовой\n" +
					"торговли,\n" +
					"без НДС(рублей)", 60),
				new PrintColumn("проц.", 30),
				new PrintColumn("рублей", 30),
				new PrintColumn("Фактическая\n" +
					"отпускная\n" +
					"цена, уста-\n" +
					"новленнаяорганизаци-\n" +
					"ей оптовой\n" +
					"торговли,\n" +
					"без НДС(рублей)", 60),
				new PrintColumn("проц.", 30),
				new PrintColumn("рублей", 30),
				new PrintColumn("проц.", 30),
				new PrintColumn("рублей", 30),
				new PrintColumn("Фактическая\n" +
					"отпускная\n" +
					"цена, уста-\n" +
					"новленная\n" +
					"организацией\n" +
					"розничной торговли,\n" +
					"без НДС (рублей)", 60),
			};
			var dataTable = BuildTableHeader(columns, new [] {
				new ColumnGroup("Размер фактической\n" +
					" оптовой надбавки, \n" +
					"установленной организацией\n" +
					" оптовой торговли", 5, 6),
				new ColumnGroup("Размер фактической\n " +
					"оптовой надбавки,\n " +
					"установленной организацией\n" +
					" оптовой торговли", 8, 9),
				new ColumnGroup("Суммарный раз-\n" +
					"мер фактических\n" +
					"оптовых надбавок,\n" +
					"установленных\n" +
					"организациями\n" +
					"розничной\n" +
					"торговли", 11, 12),
				new ColumnGroup("Размер факти-\n" +
					"ческой рознич-\n" +
					"ной надбавки,\n" +
					"установленной\n" +
					"организацией\n" +
					"розничной\n" +
					"торговли", 13, 14),
			});
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);
			var tableHeader = new TableRow();
				(new [] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11","12", "13", "14", "15", "16" })
				.Each(i => {
					var tableCell = Cell(i);
					tableCell.TextAlignment = TextAlignment.Center;
					tableCell.FontSize = 9;
					tableHeader.Cells.Add(tableCell);
				});

			dataTable.RowGroups[0].Rows.Add(tableHeader);
			var rows = lines.Select((o, i) => new object[]
			{
				o.Product,
				o.SerialNumber,
				o.Producer,
				o.SupplierCost,
				o.SupplierCostWithoutNds,
				0,
				0,
				0,
				null,
				null,
				null,
				null,
				null,
				null,
				o.ExciseTax,
				o.RetailCost
			});
			BuildRows(rows, columns, dataTable);
			doc.Blocks.Add(dataTable);

			Block(new List<Grid>
			{
				TextWithLineSign("", "(подпись уполномоченного лица поставщика - организации оптовой торговли или \n" +
					"организации розничной торговли - указать нужное)"),
				TextWithLineSign(fio, "(ФИО)"),
			});
			Block(new List<Grid>
			{
				TextWithLineSign($"{d.Date:d}", "М.П."),
			});

		}

		private void Block(List<Grid> items)
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
			grid.ColumnDefinitions[column - 1].Width = GridLength.Auto;
		}

		private Grid TextWithLineSign(string text, string sign)
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
					Margin = new Thickness(5, 0, 0, 0),
					SnapsToDevicePixels = true,
					Content = text,
					HorizontalContentAlignment = HorizontalAlignment.Center
				})
				.Cell(1, 0, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 8,
					Content = sign,
					HorizontalContentAlignment = HorizontalAlignment.Center
				});
			return grid;
		}
	}
}
