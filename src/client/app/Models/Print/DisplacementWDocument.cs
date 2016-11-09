using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Common.Tools;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Models.Print
{
	public class DisplacementWDocument : BaseDocument
	{
		private DisplacementDoc d;
		private DocumentTemplate template;
		private IList<DisplacementLine> lines;
		private WaybillSettings waybillSettings;
		private RequirementWaybillName requirementWaybillName;

		public DisplacementWDocument(DisplacementDoc d, IList<DisplacementLine> lines, WaybillSettings waybillSettings, RequirementWaybillName result)
		{
			requirementWaybillName = result;
			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			this.d = d;
			this.lines = lines;
			this.waybillSettings = waybillSettings;

			BlockStyle = new Style(typeof(Paragraph)) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
					new Setter(System.Windows.Documents.Block.MarginProperty, new Thickness(0, 3, 0, 3))
				}
			};

			HeaderStyle = new Style(typeof(Run), HeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 14d),
					new Setter(Control.FontWeightProperty, FontWeights.Normal),
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
			HeaderBold("Требование-накладная №ВП2-308");
			Header($"от {d.Date:d}");

			var header = new Grid();
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			header.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			header
				.Cell(0, 0, LeftHeaderTable())
				.Cell(0, 1, RightHeaderTable());
			doc.Blocks.Add(new BlockUIContainer(header));
			Block(new List<Grid>
			{
				Text("Затребовал"),
				TextWithLineSign(requirementWaybillName.DemandPos,"должность"),
				TextWithLineSign(requirementWaybillName.DemandFio,"фио"),
				Text("Разрешил"),
				TextWithLineSign(requirementWaybillName.ReceiptPos,"должность"),
				TextWithLineSign("              ","подпись"),
				TextWithLineSign(requirementWaybillName.ReceiptFio,"расшифровка подписи", 150),
			});
			var columns = new[] {
				new PrintColumn("Наименование", 170),
				new PrintColumn("Серия", 80),
				new PrintColumn("Срок", 80),
				new PrintColumn("наименование", 80),
				new PrintColumn("Код по ОКЕИ", 80),
				new PrintColumn("Цена", 80),
				new PrintColumn("Затребовано.", 80),
				new PrintColumn("Отпущено", 80),
				new PrintColumn("Сумма, руб", 80),
				new PrintColumn("Дебет", 80),
				new PrintColumn("Кредит", 80),
				new PrintColumn("Примечание", 80),
			};
				var dataTable = BuildTableHeader(columns, new [] {
				new ColumnGroup("Материальные ценности", 0, 2),
				new ColumnGroup("Ед.Изм.", 3, 4),
				new ColumnGroup("Кол-во", 6, 7),
				new ColumnGroup("Корр. счета", 9, 10),
			});
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);
			var tableHeader = new TableRow();
				(new [] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"})
				.Each(i => {
					var tableCell = Cell(i);
					tableCell.TextAlignment = TextAlignment.Center;
					tableHeader.Cells.Add(tableCell);
				});
			var rows = lines.Select((l, i) => new object[] {
				l.Product,
				l.SerialNumber,
				l.Period,
				"упак",
				"778",
				l.RetailCost,
				l.Quantity,
				l.Quantity,
				l.RetailSum,
				null,
				null,
				null
			});
			BuildRows(rows, columns, dataTable);

			var retailSum = lines.Sum(l => l.RetailSum);
			var result = new TableRow();
			result.FontWeight = FontWeights.Bold;
			result.FontSize = 8;
			result.Cells.Add(Cell("Итого", 5));
			result.Cells.Add(Cell("X"));
			result.Cells.Add(Cell("X"));
			result.Cells.Add(Cell("X"));
			result.Cells.Add(Cell(retailSum));
			dataTable.RowGroups[0].Rows.Add(result);
			doc.Blocks.Add(dataTable);

			var body = new Grid();
			body
				.Cell(0, 0, new Grid()
					.Cell(0, 0, BlockInner(new List<Grid> {
						Text("Отпустил"),
					}))
					.Cell(1, 0, BlockInner(new List<Grid> {
						TextWithLineSign(requirementWaybillName.ResolPos, "должность"),
						TextWithLineSign("              ", "подпись"),
						TextWithLineSign(requirementWaybillName.ResolFio, "расшифровка подписи"),
					}))
					.Cell(2, 0, BlockInner(new List<Grid> {
						Text("\"______ \""),
						Text("__________________ "),
						Text("_______года"),
					}))
					.Cell(3, 0, BlockInner(new List<Grid> {
						Text("Получил"),
					}))
					.Cell(4, 0, BlockInner(new List<Grid> {
						TextWithLineSign(requirementWaybillName.RemisPos, "должность"),
						TextWithLineSign("              ", "подпись"),
						TextWithLineSign(requirementWaybillName.RemisFio, "расшифровка подписи"),
					}))
					.Cell(5, 0, BlockInner(new List<Grid> {
						Text("\"______ \""),
						Text("__________________ "),
						Text("_______года"),
					}))
				)
				.Cell(0, 1, new Grid()
					.Cell(0, 0, BlockInner(new List<Grid> {
						Text("Ответственный исполнитель"),
					}))
					.Cell(1, 0, BlockInner(new List<Grid> {
						TextWithLineSign(requirementWaybillName.ExecutorPos, "должность"),
						TextWithLineSign("              ", "подпись"),
						TextWithLineSign(requirementWaybillName.ExecutorFio, "расшифровка подписи"),
						Text("")
					}))
					.Cell(2, 0, BlockInner(new List<Grid> {
						Text("\"______ \""),
						Text("__________________ "),
						Text("_______года"),
					})))
				.Cell(0, 2,
					new Label {
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(1, 1, 1, 1),
						SnapsToDevicePixels = true,
						Content = new Grid()
							.Cell(0, 0, BlockInner(new List<Grid> {
								Text("Отметка бухгалтерии"),
							}))
							.Cell(1, 0, BlockInner(new List<Grid> {
								Text("Корреспонденция счетов (гр. 10-11) отражена"),
							}))
							.Cell(2, 0, BlockInner(new List<Grid> {
								Text("в журнале операций за _______20__ г."),
							}))
							.Cell(3, 0, BlockInner(new List<Grid> {
								Text("Исполнитель"),
							}))
							.Cell(4, 0, BlockInner(new List<Grid> {
								TextWithLineSign("              ", "должность"),
								TextWithLineSign("         ", "подпись"),
								TextWithLineSign("              ", "расшифровка подписи"),
							}))
							.Cell(5, 0, BlockInner(new List<Grid> {
								Text("\"______ \""),
								Text("__________________ "),
								Text("_______года"),
							}))
					});
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			body.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});

			doc.Blocks.Add(new BlockUIContainer(body));
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

		private Grid TextWithLineSign(string text, string sign)
		{
			if (string.IsNullOrEmpty(text)) {
				text = " ";
			}
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
			if (string.IsNullOrEmpty(text)) {
				text = " ";
			}
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

		private Grid RightHeaderTable()
		{
			var grid = new Grid()
				.Cell(0, 0, LabelWithoutBorder(""))
				.Cell(0, 1, LabelWithBorder("Коды"))
				.Cell(1, 0, LabelWithoutBorder("Форма по ОКУД"))
				.Cell(1, 1, LabelWithBorder("504204"))
				.Cell(2, 0, LabelWithoutBorder("дата"))
				.Cell(2, 1, LabelWithBorder($"{d.Date:d}"))
				.Cell(3, 0, LabelWithoutBorder("по ОКПО"))
				.Cell(3, 1, LabelWithBorder("1910626"))
				.Cell(4, 0, LabelWithoutBorder(""))
				.Cell(4, 1, LabelWithBorder(""))
				.Cell(5, 0, LabelWithoutBorder(""))
				.Cell(5, 1, LabelWithBorder(""))
				.Cell(6, 0, LabelWithoutBorder(""))
				.Cell(6, 1, LabelWithBorder(""))
				.Cell(7, 0, LabelWithoutBorder(""))
				.Cell(7, 1, LabelWithBorder(""))
				.Cell(8, 0, LabelWithoutBorder("по ОКЕИ"))
				.Cell(8, 1, LabelWithBorder("383"));
			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}

		private Grid LeftHeaderTable()
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(1, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = GridLength.Auto,
			});
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
				grid.Cell(0, 0, new Grid()
					.Cell(0, 0, Text("Учреждение"))
					.Cell(0, 1, TextWithLine(waybillSettings == null ? "" : waybillSettings.FullName)))
				.Cell(1, 0, new Grid()
					.Cell(0, 0, Text("Структурное подразделение-отправитель"))
					.Cell(0, 1, TextWithLine($"{d.AddressName}")))
				.Cell(2, 0, new Grid()
					.Cell(0, 0, Text("Структурное подразделение-получатель"))
					.Cell(0, 1, TextWithLine($"{d.DstAddressName}")))
				.Cell(3, 0, new Grid()
					.Cell(0, 0, Text("Единица измерения"))
					.Cell(0, 1, Text("руб (с точностью до второго десятичного знака)")))
				.Cell(4, 0, new Grid()
					.Cell(0, 0, Text("Учреждение"))
					.Cell(0, 1, TextWithLine(waybillSettings == null ? "" : waybillSettings.FullName)))
				;
			return  grid;
		}

		private Label LabelWithoutBorder(string text)
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
		private Label LabelWithBorder(string text)
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

		private void TwoColumns()
		{
			template = new DocumentTemplate();
		}

		public override FrameworkContentElement GetHeader(int page, int pageCount)
		{
			return null;
		}

		public override FrameworkContentElement GetFooter(int page, int pageCount)
		{
			return new Paragraph(new Run($"страница {page + 1} из {pageCount}")) {
				FontFamily = new FontFamily("Arial"),
				FontSize = 8
			};
		}

		protected override Paragraph Block(string text)
		{
			var block = base.Block(text);
			CheckTemplate(block);
			return block;
		}

		public override Paragraph Header(string text)
		{
			var block = base.Header(text);
			CheckTemplate(block);
			return block;
		}

		public Paragraph HeaderBold(string text)
		{
			var block = base.Header(text);
			CheckTemplate(block);
			block.FontWeight = FontWeights.Bold;
			return block;
		}

		private void CheckTemplate(Paragraph block)
		{
			if (template != null) {
				doc.Blocks.Remove(block);
				Stash(block);
			}
		}

		private void Stash(FrameworkContentElement element)
		{
			template.Parts.Add(element);
			if (template.IsReady) {
				doc.Blocks.Add(template.ToBlock());
				template = null;
			}
		}
	}
}