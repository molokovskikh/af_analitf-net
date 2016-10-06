using AnalitF.Net.Client.Models.Inventory;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	class WriteoffActDocument : BaseDocument
	{
		private WriteoffLine[] _items;
		private BlockUIContainer headerBlock;

		public WriteoffActDocument(WriteoffLine[] items)
		{
			_items = items;
		}

		protected override void BuildDoc()
		{
			doc.Blocks.Add(new BlockUIContainer(HeaderTable(0)));

			// сумма 690
			var headers = new[]
			{
				new PrintColumn("Товар", 240),
				new PrintColumn("Производитель", 200),
				new PrintColumn("Серия", 50),
				new PrintColumn("Срок годности", 50),
				new PrintColumn("Кол-во", 50),
				new PrintColumn("Цена", 50),
				new PrintColumn("Стоимость", 50),
			};

			var rows = _items.Select((o, i) => new object[]
			{
				o.Product,
				o.Producer,
				o.SerialNumber,
				o.Period,
				o.Quantity,
				o.RetailCost,
				o.RetailSum,
			});

			var table = BuildTable(rows, headers);
			var totalSum = _items.Sum(l => l.RetailSum);
			if (_items.Length > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
						},
						new TableCell(new Paragraph(new Run(totalSum.ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Right
						},
					}
				});
			}

			doc.Blocks.Add(new BlockUIContainer(new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = "Сумма списания _______________________________________________________________________________",
					TextWrapping = TextWrapping.Wrap
				}
			}));
			doc.Blocks.Add(new BlockUIContainer(new Grid()
				.Cell(0, 0, SingBlock("Председатель комиссии"))
				.Cell(1, 0, SingBlock("Члены комиссии"))
				.Cell(2, 0, SingBlock(""))
				.Cell(3, 0, SingBlock("Мат.ответственное лицо"))
			));
		}

		private static Grid HeaderTable(int code)
		{
			var header = new Grid();
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header.Cell(0, 0, new Grid()
					.Cell(0, 0, SingBlock("", "(организация, номер телефона)"))
					.Cell(1, 0, SingBlock("", ""))
					.Cell(2, 0, SingBlock("", "(структурное подразделение)"))
					.Cell(3, 0, SingBlock("Основание для составления акта", "", 300))
				)
				.Cell(0, 1, RightHeaderTable(code)
				)
				.Cell(1, 0, Header())
				.Cell(1, 1, Caption()
				);
			return header;
		}

		private static Grid Header()
		{
			var grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());

			grid.Cell(0, 0, Head());

			grid.Children[0].SetValue(Grid.RowProperty, 1);
			grid.Children[0].SetValue(Grid.ColumnProperty, 0);

			return grid;
		}

		private static Grid Caption()
		{
			var grid = new Grid();
			grid.Cell(0, 0, LabelWithoutBorder("УТВЕРЖДАЮ"))
				.Cell(1, 0, LabelWithoutBorder("Руководитель"))
				.Cell(2, 0, SingBlock("", "(должность)"))
				.Cell(3, 0, new Grid()
					.Cell(0, 0, SingBlock("", "(подпись)"))
					.Cell(0, 1, SingBlock("", "  (расшифровка подписи)  ")))
				.Cell(4, 0, new Grid()
					.Cell(0, 0, LabelWithoutBorder("<<______>>"))
					.Cell(0, 1, LabelWithoutBorder("__________ _____г."))
				);
			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}

		private static Grid Head()
		{
			var grid = new Grid().Cell(1, 0, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 14,
					FontWeight = FontWeights.Bold,
					SnapsToDevicePixels = true,
					Content = "о списании товара",
					HorizontalAlignment = HorizontalAlignment.Center
				})
				.Cell(0, 1, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = "Номер документа"
				})
				.Cell(0, 2, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = "Дата составления"
				})
				.Cell(1, 1, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = ""
				})
				.Cell(1, 2, new Label
				{
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1, 1, 1, 1),
					SnapsToDevicePixels = true,
					FontSize = 9,
					Content = DateTime.Now.ToString("dd/M/yyyy")
				})
				.Cell(0, 0, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 14,
					FontWeight = FontWeights.Bold,
					SnapsToDevicePixels = true,
					Content = "АКТ",
					HorizontalAlignment = HorizontalAlignment.Center
				});
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			return grid;
		}

		private static Grid RightHeaderTable(int code)
		{
			var grid = new Grid()
				.Cell(0, 0, LabelWithoutBorder(""))
				.Cell(0, 1, LabelWithBorder("Код"))
				.Cell(1, 0, LabelWithoutBorder("Форма по ОКУД"))
				.Cell(1, 1, LabelWithBorder(code.ToString()))
				.Cell(2, 0, LabelWithoutBorder("по ОКПО"))
				.Cell(2, 1, LabelWithBorder(""))
				.Cell(3, 0, LabelWithoutBorder("ИНН"))
				.Cell(3, 1, LabelWithBorder(""))
				.Cell(4, 0, LabelWithoutBorder(""))
				.Cell(4, 1, LabelWithBorder(""))
				.Cell(5, 0, LabelWithoutBorder("Вид деятельности по ОКДП"))
				.Cell(5, 1, LabelWithBorder(""))
				.Cell(6, 0, LabelWithoutBorder("Номер производителя"))
				.Cell(6, 1, LabelWithBorder(""))
				.Cell(7, 0, LabelWithoutBorder("Номер регистрационный"))
				.Cell(7, 1, LabelWithBorder(""))
				.Cell(8, 0, LabelWithoutBorder(""))
				.Cell(8, 1, LabelWithBorder(""))
				.Cell(9, 0, LabelWithoutBorder("Кассир"))
				.Cell(9, 1, LabelWithBorder(""))
				.Cell(10, 0, LabelWithoutBorder("Вид операции"))
				.Cell(10, 1, LabelWithBorder(""));
			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}

		private static Label LabelWithoutBorder(string text)
		{
			return new Label
			{
				Content = new TextBlock
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
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
					FontSize = 12,
					Text = text,
					TextWrapping = TextWrapping.Wrap,
					Width = 90,
					TextAlignment = TextAlignment.Center
				},
			};
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
				FontSize = 9,
				Content = signature,
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private static Grid SingBlock(string text, string signature, int size)
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
				Width = size,
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = signature,
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private static Grid SingBlock(string name)
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
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});

			grid.Cell(0, 0, new Label {
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = name,
					TextWrapping = TextWrapping.Wrap,
					Width = 200
				},
			});

			grid.Cell(0, 1, new Label {
				Width = 100,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(должность)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});

			grid.Cell(0, 2, new Label
			{
				Width = 87,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 2, new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(подпись)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});

			grid.Cell(0, 3, new Label {
				BorderBrush = Brushes.Black,
				Width = 250,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 3, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(ф.и.о.)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}

		private static Grid SingBlock()
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
				Width = 200,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 0, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(должность)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			grid.Cell(0, 1, new Label {
				Width = 87,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 1, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(подпись)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			grid.Cell(0, 2, new Label {
				BorderBrush = Brushes.Black,
				Width = 350,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Margin = new Thickness(5, 0, 5, 0),
			});
			grid.Cell(1, 2, new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = "(ф.и.о.)",
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			return grid;
		}
	}
}
