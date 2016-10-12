using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	public class ReturnWaybill : BaseDocument
	{
		private ReturnToSupplier returnToSupplier;
		private WaybillSettings waybillSettings;
		public ReturnWaybill(ReturnToSupplier _returnToSupplier, WaybillSettings _waybillSettings)
		{
			returnToSupplier = _returnToSupplier;
			waybillSettings = _waybillSettings;
			doc.PagePadding = new Thickness(29);
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
			var waybillId = returnToSupplier.Lines.Count == 0 ? "        " : returnToSupplier.Lines.First().Stock.WaybillId.ToString();
			var waybillDate = returnToSupplier.Lines.Count == 0 ? "       " : returnToSupplier.Lines.First().Stock.DocumentDate;

			var right = Block("Унифицированная форма № Торг-12 \n" +
			                  "Утверждена постановлением Госкомстата России от 25.12.98 № 132");
			right.TextAlignment = TextAlignment.Right;
			right.FontSize = 6;
			right.FontStyle = FontStyles.Italic;

			var header = new Grid();
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header
				.Cell(0, 0, new Grid()
					.Cell(0, 0, BlockInner(new List<Grid>
					{
						Text("Грузоотправитель", 120),
						TextWithLineSign($"{returnToSupplier.SupplierName}, {returnToSupplier.AddressName}; ИНН/КПП /; Р/С ; К/С ; БИК ",
						"организация, адрес, номер телефона, банковские реквизиты"),
					}))
					.Cell(1, 0, BlockInner(new List<Grid>
					{
						Text("Грузополучатель", 120),
						TextWithLineSign($"{waybillSettings.FullName}, {waybillSettings.Address}; ИНН/КПП /; Р/С ; К/С ; БИК ",
						"организация, адрес, номер телефона, банковские реквизиты"),
					}))
					.Cell(2, 0, BlockInner(new List<Grid>
					{
						Text("Поставщик", 120),
						TextWithLineSign($"{returnToSupplier.SupplierName}, {returnToSupplier.AddressName}; ИНН/КПП /; Р/С ; К/С ; БИК ",
						"организация, адрес, номер телефона, банковские реквизиты"),
					}))
					.Cell(3, 0, BlockInner(new List<Grid>
					{
						Text("Плательщик", 120),
						TextWithLineSign($"{waybillSettings.FullName}, {waybillSettings.Address}; ИНН/КПП /; Р/С ; К/С ; БИК ",
						"организация, адрес, номер телефона, банковские реквизиты"),
					}))
					.Cell(4, 0, BlockInner(new List<Grid>
					{
						Text("Основание", 120),
						TextWithLineSign("брак при премке ", "договор, заказ-наряд"),
					})))
				.Cell(0, 1, RightHeaderTable())
				.Cell(1, 0, CaptionTable());
			doc.Blocks.Add(new BlockUIContainer(header));
			Block(new List<Grid> {Text("")});
			var columns = new[] {
				new PrintColumn("№п/п", 20),
				new PrintColumn("Наименование, характеристика, сорт, артикул товара, поставщик", 150),
				new PrintColumn("Код", 30),
				new PrintColumn("Наим-ие", 20),
				new PrintColumn("Код по ОКЕИ", 20),
				new PrintColumn("Вид упаковки", 20),
				new PrintColumn("в одном месте", 20),
				new PrintColumn("мест, шт.", 20),
				new PrintColumn("масса, брутто", 20),
				new PrintColumn("Кол-во (масса, нетто)", 40),
				new PrintColumn("Цена,руб.коп.", 40),
				new PrintColumn("Цена,с НДС", 40),
				new PrintColumn("Сумма,без учета НДС", 40),
				new PrintColumn("ставка, %", 30),
				new PrintColumn("Сумма", 50),
				new PrintColumn("Сумма,с учетом НДС", 50),
				new PrintColumn("Серия, срок годности", 50),
				new PrintColumn("Рег.номер,Сертификат", 80),
			};
			var dataTable = BuildTableHeader(columns, new [] {
				new ColumnGroup("Товар", 1, 2),
				new ColumnGroup("Ед.Изм.", 3, 4),
				new ColumnGroup("Кол-во", 6, 7),
				new ColumnGroup("НДС", 12, 13),
			});
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);
			var tableHeader = new TableRow();
				(new [] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", " " ,"12", "13", "14", "15" })
				.Each(i => {
					var tableCell = Cell(i);
					tableCell.TextAlignment = TextAlignment.Center;
					tableCell.FontSize = 9;
					tableHeader.Cells.Add(tableCell);
				});

			dataTable.RowGroups[0].Rows.Add(tableHeader);
			var rows = returnToSupplier.Lines.Select((o, i) => new object[]
			{
				i+1,
				o.Product + o.Producer + "Прдок" + o.Stock.WaybillId + "от " + o.Stock.DocumentDate,
				o.Id,
				"упак",
				"778",
				null,
				null,
				null,
				null,
				o.Quantity,
				o.SupplierCostWithoutNds,
				o.SupplierCost,
				o.SupplierSumWithoutNds,
				o.Stock.Nds,
				o.Stock.NdsAmount,
				o.SupplierSum,
				o.Stock.SerialNumber + " " + o.Stock.Period,
				o.Stock.Id + " " + o.Stock.Certificates
			});
			BuildRows(rows, columns, dataTable);
			var result = new TableRow();
			result.FontWeight = FontWeights.Bold;
			result.FontSize = 8;
			result.Cells.Add(Cell("Всего по накладной", 9));
			result.Cells.Add(Cell(returnToSupplier.Lines.Sum(l => l.Quantity)));
			result.Cells.Add(Cell("X"));
			result.Cells.Add(Cell("X"));
			result.Cells.Add(Cell(returnToSupplier.Lines.Sum(l => l.SupplierSumWithoutNds)));
			result.Cells.Add(Cell("X"));
			result.Cells.Add(Cell(returnToSupplier.Lines.Sum(l => l.Stock.NdsAmount)));
			result.Cells.Add(Cell(returnToSupplier.Lines.Sum(l => l.SupplierSum)));
			dataTable.RowGroups[0].Rows.Add(result);
			doc.Blocks.Add(dataTable);

			Block(new List<Grid>
			{
				Text("Товарная накладная имеет приложение на"),
				TextWithLine("", 300),
				Text("листах")
			});
			Block(new List<Grid>
			{
				Text("и содержит"),
				TextWithLineSign("","прописью", 445),
				Text("порядковых номеров записей")
			});
			Block(new List<Grid>
			{
				Text("Всего мест"),
				TextWithLineSign("","прописью", 300),
				Text("Масса груза (нетто)"),
				TextWithLineSign("","прописью"),
			});
			Block(new List<Grid>
			{
				Text("", 355),
				Text("Масса груза (брутто)"),
				TextWithLineSign("","прописью"),
			});
			Block(new List<Grid>
			{
				Text("Приложение (паспорта, сертификаты, и т. п.) на"),
				TextWithLineSign("","прописью", 150),
				Text("листах"),
			});
			var body = new Grid();
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(50)
			});
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			body
				.Cell(0, 0, BlockInner(new List<Grid>
				{
					Text("Всего отпущено на сумму"),
					TextWithLineSign("","прописью"),
				}))
				.Cell(1, 0, BlockInner(new List<Grid>
				{
					TextWithLine(""),
				}))
				.Cell(2, 0, BlockInner(new List<Grid>
				{
					Text("Отпуск разрешил"),
					TextWithLineSign("директор","должность"),
					TextWithLineSign("","подпись"),
					TextWithLineSign("","расшифровка подписи"),
				}))
				.Cell(3, 0, BlockInner(new List<Grid>
				{
					Text("Главный (старший) бухгалтер"),
					TextWithLineSign("","подпись"),
					TextWithLineSign("","расшифровка подписи"),
				}))
				.Cell(4, 0, BlockInner(new List<Grid>
				{
					Text("Отпуск груза"),
					TextWithLineSign("директор","должность"),
					TextWithLineSign("","подпись"),
					TextWithLineSign("","расшифровка подписи"),
				}))
				.Cell(5, 0, BlockInner(new List<Grid>
				{
					Text("М. П."),
					Text("", 150),
					TextWithLine(DateTime.Now.ToString("dd/M/yyyy")),
				}))
				.Cell(0, 2, BlockInner(new List<Grid>
				{
					Text("По доверенности №____________________ от \"   \"_____________г"),
				}))
				.Cell(1, 2, BlockInner(new List<Grid>
				{
					Text("")
				}))
				.Cell(2, 2, BlockInner(new List<Grid>
				{
					Text("Выданной"),
					TextWithLineSign("","кем, кому (организация, должность, фамилия, и. о.)"),
				}))
				.Cell(3, 2, BlockInner(new List<Grid>
				{
					Text("Груз принял"),
					TextWithLineSign("","должность"),
					TextWithLineSign("","подпись"),
					TextWithLineSign("","расшифровка подписи"),
				}))
				.Cell(4, 2, BlockInner(new List<Grid>
				{
					Text("Груз получил"),
					TextWithLineSign("","должность"),
					TextWithLineSign("","подпись"),
					TextWithLineSign("","расшифровка подписи"),
				}))
				.Cell(5, 2, BlockInner(new List<Grid>
				{
					Text("М. П."),
					Text("", 150),
					Text("\"  \"______________20   г"),
				}))
				.Cell(0, 1, BlockInner(new List<Grid>
				{
					Text("", 50),
				}));
			doc.Blocks.Add(new BlockUIContainer(body));
		}

		private Grid RightHeaderTable()
		{
			var grid = new Grid();
			grid
				.Cell(0, 1, LabelWithoutBorder(""))
				.Cell(0, 2, LabelWithBorder("Коды"))
				.Cell(1, 1, LabelWithoutBorder("Форма по ОКУД"))
				.Cell(1, 2, LabelWithBorder("330202"))
				.Cell(2, 1, LabelWithoutBorder("по ОКПО"))
				.Cell(2, 2, LabelWithBorder(returnToSupplier.SupplierName))
				.Cell(3, 1, LabelWithoutBorder(""))
				.Cell(3, 2, LabelWithBorder(""))
				.Cell(4, 1, LabelWithoutBorder("Вид деятельности по ОКДП"))
				.Cell(4, 2, LabelWithBorder(""))
				.Cell(5, 1, LabelWithoutBorder("по ОКПО"))
				.Cell(5, 2, LabelWithBorder(""))
				.Cell(6, 1, LabelWithoutBorder("по ОКПО"))
				.Cell(6, 2, LabelWithBorder(returnToSupplier.SupplierName))
				.Cell(7, 1, LabelWithoutBorder("по ОКПО"))
				.Cell(7, 2, LabelWithBorder(""))
				.Cell(8, 1, LabelWithBorderLeft("номер"))
				.Cell(8, 2, LabelWithBorder(""))
				.Cell(9, 1, LabelWithBorderLeft("дата"))
				.Cell(9, 2, LabelWithBorder(""))
				.Cell(10, 1, LabelWithBorderLeft("номер"))
				.Cell(10, 2, LabelWithBorder(""))
				.Cell(11, 1, LabelWithBorderLeft("дата"))
				.Cell(11, 2, LabelWithBorder(""))
				.Cell(12, 1, LabelWithoutBorder("Вид операции"))
				.Cell(12, 2, LabelWithBorder(""))
				.Cell(10, 0, LabelWithoutBorder("Транспортная"))
				.Cell(11, 0, LabelWithoutBorder("накладная"));
			return grid;
		}

		private static Label LabelWithoutBorder(string text)
		{
			return new Label
			{
				Content = new TextBlock
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 8,
					Text = text,
					TextWrapping = TextWrapping.Wrap,
				},
				HorizontalContentAlignment = HorizontalAlignment.Right
			};
		}
		private static Label LabelWithBorder(string text)
		{
			return new Label
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1, 1, 1, 1),
				SnapsToDevicePixels = true,
				Content = new TextBlock
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 8,
					Text = text,
					TextWrapping = TextWrapping.Wrap,
					TextAlignment = TextAlignment.Center,
					Width = 180
				},
				HorizontalAlignment = HorizontalAlignment.Right
			};
		}
		private static Label LabelWithBorderLeft(string text)
		{
			return new Label
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1, 1, 0, 1),
				SnapsToDevicePixels = true,
				Content = new TextBlock
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 8,
					Text = text,
					TextWrapping = TextWrapping.Wrap,
					TextAlignment = TextAlignment.Right,
					Width = 40
				},
				HorizontalAlignment = HorizontalAlignment.Right
			};
		}

		private Grid SingBlockHeader(string text, string signature)
		{
			var grid = new Grid();
			grid.Cell(0, 1, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
				Content = text,
				Width = 500,
				FontWeight = FontWeights.Bold,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 6,
				Content = signature,
				Width = 500,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			return grid;
		}
		private Grid SingBlockHeaderLabel(string label, string text)
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			grid.Cell(0, 0, new Label {
				Content = label,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.Cell(0, 1, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
				Content = text,
				HorizontalContentAlignment = HorizontalAlignment.Center
			});
			grid.HorizontalAlignment = HorizontalAlignment.Left;
			return grid;
		}
		private static Grid SingBlock(string text, string signature)
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});

			grid.Cell(0, 0, new Label {
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = text,
					TextWrapping = TextWrapping.Wrap
				},
			});
			grid.Cell(0, 1, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 6,
				Content = signature,
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}
		private Grid CaptionTable()
		{
			var grid = new Grid().Cell(1, 0, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 9,
					FontWeight = FontWeights.Bold,
					SnapsToDevicePixels = true,
					Content = "ТОВАРНАЯ НАКЛАДНАЯ",
					HorizontalAlignment = HorizontalAlignment.Center
				})
				.Cell(0, 1, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 8,
					Content = "Номер документа"
				})
				.Cell(0, 2, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 8,
					Content = "Дата составления"
				})
				.Cell(1, 1, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 8,
					Content = "89"
				})
				.Cell(1, 2, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 8,
					Content = DateTime.Now.ToString("dd/M/yyyy")
				})
				.Cell(0, 0, new Label
				{
					SnapsToDevicePixels = true,
				});
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			return grid;
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
			grid.ColumnDefinitions[column - 1].Width = new GridLength(1, GridUnitType.Star);
		}

		private Grid BlockInner(List<Grid> items)
		{
			var grid = new Grid();
			var column = 0;
			foreach (var item in items)
			{
				grid.Cell(0, column, item);
				grid.ColumnDefinitions[column].Width = GridLength.Auto;
				column++;
			}
			grid.ColumnDefinitions[column - 1].Width = new GridLength(1, GridUnitType.Star);
			return grid;
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

		private Grid Text(string text, int size)
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
					TextWrapping = TextWrapping.Wrap,
					Width = size,
					Text = text
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
		private Grid TextWithLine(string text, int size)
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
					Width = size
				});
			return grid;
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
		private Grid TextWithLineSign(string text, string sign, int size)
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
					Width = size
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
		private Grid SingBlock()
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			grid.Cell(0, 0, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 5, 5, 0),
			});
			grid.Cell(1, 0, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 6,
				Content = "место работы, должность",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				Width = 280
			});
			grid.Cell(0, 1, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 5, 5, 0),
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 6,
				Content = "подпись",
				HorizontalContentAlignment = HorizontalAlignment.Center,
				Width = 120
			});
			grid.Cell(0, 2, new Label {
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 5, 5, 0),
			});
			grid.Cell(1, 2, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 6,
				Content = "расшифровка подписи",
				HorizontalContentAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}
	}
}
