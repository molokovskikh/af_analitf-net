using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Common.Tools;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	public class DisplacementWDocument : BaseDocument
	{
		private DisplacementDoc d;
		private DocumentTemplate template;
		private IList<DisplacementLine> lines;
		private WaybillSettings waybillSettings;

		public DisplacementWDocument(DisplacementDoc d, IList<DisplacementLine> lines, WaybillSettings waybillSettings)
		{
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
			header.RowDefinitions.Add(new RowDefinition());
			header.RowDefinitions.Add(new RowDefinition());
			header
				.Cell(0, 0, LeftHeaderTable())
				.Cell(0, 1, RightHeaderTable());
			doc.Blocks.Add(new BlockUIContainer(header));


			Header($"\n                               Наименование организации: \n"
				+ "             Отдел:______________________________________________\n");

			TwoColumns();
			var left = Header("Требование №________________________\n"
				+ "от \"_____\" _______________20__г\n"
				+ "кому: ___________________________________\n"
				+ "Основание отпуска________________________\n");
			left.TextAlignment = TextAlignment.Center;
			Header($"Накладная № {d.Id}\n"
				+ $"от  {d.Date:d}\n"
				+ $"Через кого   \n"
				+ "Доверенность № _______от \"______\" ________20__г\n");

			var columns = new[] {
				new PrintColumn("№ пп", 27),
				new PrintColumn("Наименование", 170),
				new PrintColumn("Производитель", 170),
				new PrintColumn("Серия", 80),
				new PrintColumn("Срок", 80),
				new PrintColumn("Цена", 80),
				new PrintColumn("Затребован.колич.", 80),
				new PrintColumn("Отпущен.колич.", 80),
				new PrintColumn("Сумма, руб", 80),
			};
			var rows = lines.Select((l, i) => new object[] {
				++i,
				l.Product,
				l.Producer,
				l.SerialNumber,
				l.Period,
				l.RetailCost,
				l.Quantity,
				l.Quantity,
				l.RetailSum
			});
			BuildTable(rows, columns);

			var retailSum = lines.Sum(l => l.RetailSum);
			var block = Block("Продажная сумма: " + RusCurrency.Str((double)retailSum));
			block.Inlines.Add(new Figure(new Paragraph(new Run(retailSum.ToString()))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});

			TwoColumns();
			Block($"Затребовал:  \n\n"
				+ "место печати       подпись     _________________\n\n"
				+ "\" ____\" _______________20__г\n");
			Block($"Отпустил: Сдал (выдал)________________\n\n"
				+ $"Получил:Принял(получил)______________\n\n"
				+ $"Руководитель учреждения_____________\n\n"
				+ $"Главный (старший)бухгалтер ________________\n");
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
			var grid = new Grid()
				.Cell(0, 0, new Grid()
					.Cell(0, 0, Text("Учреждение"))
					.Cell(0, 1, TextWithLine(waybillSettings == null ? "" : waybillSettings.FullName)))
				.Cell(0, 1, new Grid()
					.Cell(0, 0, Text("Структурное подразделение-\n" +
						"отправитель"))
					.Cell(0, 1, TextWithLine($"{d.AddressName}")))
				.Cell(0, 2, new Grid()
					.Cell(0, 0, Text("Структурное подразделение-\n" +
						"получатель"))
					.Cell(0, 1, TextWithLine($"{d.DstAddressName}")))
				.Cell(0, 3, new Grid()
					.Cell(0, 0, Text("Единица измерения"))
					.Cell(0, 1, Text("руб (с точностью до второго десятичного знака)")))
				.Cell(0, 4, new Grid()
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