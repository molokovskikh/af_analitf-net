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
	public class ReturnDivergenceAct : BaseDocument
	{
		private DocumentTemplate template;
		private ReturnToSupplier returnToSupplier;
		private WaybillSettings waybillSettings;

		public ReturnDivergenceAct(ReturnToSupplier returnToSupplier, WaybillSettings waybillSettings)
		{
			this.returnToSupplier = returnToSupplier;
			this.waybillSettings = waybillSettings;

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
			var waybillDate = returnToSupplier.Lines.Count == 0 ? "       " : returnToSupplier.Lines.First().Stock.DocumentDate.ToShortDateString();

			var right = Block("Унифицированная форма № Торг-12 \n" +
			                  "Утверждена постановлением Госкомстата \n" +
												"России от 25.12.98 № 132");
			right.TextAlignment = TextAlignment.Right;
			right.FontSize = 10;
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
				.Cell(0, 0, LeftHeaderTable())
				.Cell(0, 1, RightHeaderTable())
				.Cell(1, 0, Caption())
				.Cell(1, 1, CaptionSignature());;
			doc.Blocks.Add(new BlockUIContainer(header));

			Block(new List<Grid>
			{
				Text("Место приемки товара"),
				TextWithLine(""),
			});
			Block(new List<Grid>
			{
				Text("Настоящий акт составлен комиссией, которая установила:"),
				Text(DateTime.Now.ToString("dd/M/yyyy"))
			});
			Block(new List<Grid>
			{
				Text("по сопроводительным документам"),
				TextWithLine($"товарная накладная №ПрДок{waybillId} от {waybillDate}")
			});
			Block(new List<Grid>
			{
				Text("доставлен товар. Документ о вызове представителя"),
				TextWithLineSign("грузоотправителя, поставщика, производителя:","ненужное зачеркнуть")
			});
			Block(new List<Grid>
			{
				TextWithLineSign("телеграмма, факс, телефонограмма, радиограмма","ненужное зачеркнуть"),
				Text("№__________ от «       »_____________             года")
			});
			Block(new List<Grid>
			{
				Text("Грузоотправитель"),
				TextWithLine(waybillSettings == null ? "" : waybillSettings.FullName + ", " + waybillSettings.Address)
			});
			Block(new List<Grid>
			{
				Text("Производитель"),
				TextWithLine(returnToSupplier.Lines.Count == 0 ? "" : returnToSupplier.Lines.First().Producer)
			});
			Block(new List<Grid>
			{
				Text("Поставщик"),
				TextWithLine(returnToSupplier == null ? "" : returnToSupplier.SupplierName + ", " + returnToSupplier.AddressName)
			});
			Block(new List<Grid>
			{
				Text("Страховая компания"),
				TextWithLineSign("","наименование, адрес, номер телефона, факса")
			});
			Block(new List<Grid>
			{
				Text("Договор (контракт) на поставку товара №___________ от «       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("Счет-фактура №"),
				TextWithLine("ПрДок" + waybillId),
				Text("от "),
				TextWithLine(waybillDate)
			});
			Block(new List<Grid>
			{
				Text("Коммерческий акт №___________ от «       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("Ветеринарное свидетельство (свидетельство) №___________ от «       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("Железнодорожная накладная №___________ от «       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("Способ доставки"),
				TextWithLineSign("","вид транспортного средства"),
				Text("№"),
				TextWithLine("")
			});
			Block(new List<Grid>
			{
				Text("Дата отправления товара «       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("со станции (пристани, порта) отправления"),
				TextWithLineSign("","наименование")
			});
			Block(new List<Grid>
			{
				Text("или со склада отправителя товара"),
				TextWithLineSign("","наименование")
			});

			Block(new List<Grid>
			{
				Text("Сведения о состоянии вагонов, автофургонов и т. д. Наличие, описание упаковочных ярлыков, пломб транспорта на" +
				     " отдельных местах (сертификатов, спецификаций в вагоне, контейнере) и отправительская \n маркировка"),
			});
			Block(new List<Grid>
			{
				Text("По сопроводительным транспортным документам значится:"),
			});

			var columns = new[] {
				new PrintColumn("Отметка об опломбировании товара (груза), состояние пломб и содержание оттиска", 240),
				new PrintColumn("Количество мест", 60),
				new PrintColumn("Вид упаковки", 80),
				new PrintColumn("Наименование товара (груза) или номера вагонов (контейнеров,автофургонов и т. д.)", 120),
				new PrintColumn("Единица измерения", 60),
				new PrintColumn("Отправителя", 80),
				new PrintColumn("Транспортной организации (станции, пристани, порта)", 80),
				new PrintColumn("Особые отметки отправителя по накладной", 120),
			};
			var dataTable = BuildTableHeader(columns, new [] {
				new ColumnGroup("Масса брутто товара (груза) по документам", 5, 6),
			});
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);
			var tableHeader = new TableRow();
				(new [] { "1", "2", "3", "4", "5", "6", "7", "8" })
				.Each(i => {
					var tableCell = Cell(i);
					tableCell.TextAlignment = TextAlignment.Center;
					tableCell.FontSize = 8;
					tableHeader.Cells.Add(tableCell);
				});

			dataTable.RowGroups[0].Rows.Add(tableHeader);
			BuildRows(new List<object[]>(), columns, dataTable);
			doc.Blocks.Add(dataTable);


			columns = new[] {
				new PrintColumn("Тип (наименование)", 240),
				new PrintColumn("Номер места", 50),
				new PrintColumn("Наименование", 50),
				new PrintColumn("код по ОКЕИ", 50),
				new PrintColumn("Артикул товара", 50),
				new PrintColumn("Сорт", 50),
				new PrintColumn("Количество (масса)", 50),
				new PrintColumn("Цена руб.коп.", 50),
				new PrintColumn("Сумма руб.коп.", 50),
			};
			dataTable = BuildTableHeader(columns, new [] {
				new ColumnGroup("Единица измерения", 2, 3),
				new ColumnGroup("По документам поставщика значится", 4, 8),
			});
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);
			tableHeader = new TableRow();
				(new [] { "1", "2", "3", "4", "5", "6", "7", "8", "9" })
				.Each(i => {
					var tableCell = Cell(i);
					tableCell.TextAlignment = TextAlignment.Center;
					tableCell.FontSize = 8;
					tableHeader.Cells.Add(tableCell);
				});
			dataTable.RowGroups[0].Rows.Add(tableHeader);
			var rows = returnToSupplier.Lines.Select((o, i) => new object[]
			{
				o.Product,
				null,
				null,
				null,
				null,
				null,
				o.Quantity,
				o.SupplierCost,
				o.SupplierSum
			});
			BuildRows(rows, columns, dataTable);
			doc.Blocks.Add(dataTable);


			Block(new List<Grid>
			{
				Text("Условия хранения товара (продукции) до его вскрытия на складе получателя:"),
				TextWithLine("")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Сведения о температуре при разгрузке в вагоне (рефрижераторе и т. д.) в товаре, ºC"),
				TextWithLine(""),
			});
			Block(new List<Grid>
			{
				Text("Состояние тары и упаковки, маркировка мест, товара и тары в момент внешнего осмотра товара (продукции)"),
				TextWithLine("")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Содержание наружной маркировки тары и другие данные, на основании которых можно сделать выводы о том, " +
				     "в чьей упаковке предъявлен товар (производителя или отправителя)"),
				TextWithLine(""),
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Дата вскрытия тары (тарного места, вагона, контейнера и т. п.) «       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("Организация, которая взвесила и опломбировала отгруженный товар, исправность пломб и содержание оттисков," +
				     " соответствие пломб товаросопроводительным документам"),
				TextWithLine(""),
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Порядок отбора товара (продукции) для выборочной проверки с указанием ГОСТ, особых условий поставки по " +
				     "договору (контракту), основание выборочной проверки:"),
				TextWithLine(""),
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {Text("")});
			columns = new[] {
				new PrintColumn("Артикул", 40),
				new PrintColumn("сорт", 40),
				new PrintColumn("кол-во", 40),
				new PrintColumn("цена", 40),
				new PrintColumn("сумма", 40),
				new PrintColumn("кол-во", 40),
				new PrintColumn("сумма", 40),
				new PrintColumn("кол-во", 40),
				new PrintColumn("сумма", 40),
				new PrintColumn("кол-во", 40),
				new PrintColumn("сумма", 40),
				new PrintColumn("кол-во", 40),
				new PrintColumn("сумма", 40),
				new PrintColumn("Номер паспорта", 80),
			};
			dataTable = BuildTableHeader(columns, new [] {
				new ColumnGroup("Фактически оказалось", 0, 4),
				new ColumnGroup("Брак", 5, 6),
				new ColumnGroup("Бой", 7, 8),
				new ColumnGroup("Недостача", 9, 10),
				new ColumnGroup("Излишки", 11, 12),
				new ColumnGroup("Отклонение", 9, 12),
			});
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);
			tableHeader = new TableRow();
				(new [] { "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" })
				.Each(i => {
					var tableCell = Cell(i);
					tableCell.TextAlignment = TextAlignment.Center;
					tableCell.FontSize = 8;
					tableHeader.Cells.Add(tableCell);
				});
			dataTable.RowGroups[0].Rows.Add(tableHeader);
			rows = returnToSupplier.Lines.Select((o, i) => new object[]
			{
				null,
				null,
				o.Quantity,
				o.SupplierCost,
				o.SupplierSum,
				null, //Брак количество
				null, //Брак сумма
				null,//бой кол-во
				null,//бой сумма
				null,// недостача кол-во
				null,// недостача сумма
				null,// излишки кол-во
				null,//излишки сумма
				null //Номер паспорта
			});
			BuildRows(rows, columns, dataTable);
			doc.Blocks.Add(dataTable);

			Block(new List<Grid>
			{
				Text("Определение количества (массы) товара (продукции) проводилось"),
				TextWithLineSign("","место определения количества (массы) товара (продукции)")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Другие данные"),
				TextWithLine("")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("По остальным товарно-материальным ценностям, перечисленным в сопроводительных документах поставщика, расхождений " +
				     "в количестве и качестве нет."),
			});
			Block(new List<Grid>
			{
				Text("Подробное описание дефектов (характер недостачи, излишков, ненадлежащего качества, брака, боя) и мнение комиссии о " +
				     "причинах их образования"),
				TextWithLine("")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Заключение комиссии"),
				TextWithLine("возврат поставщику")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("ПРИЛОЖЕНИЕ:"),
				TextWithLine("")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Члены комиссии предупреждены об ответственности за подписание акта, содержащего данные, не соответствующие " +
				     "действительности."),
			});
			//форма подписей
			Block(new List<Grid>
			{
				Text("Председатель комиссии:", 150),
				SingBlock()
			});
			Block(new List<Grid>
			{
				Text("Члены комиссии:", 150),
				SingBlock()
			});
			Block(new List<Grid>
			{
				Text("", 150),
				SingBlock()
			});
			Block(new List<Grid>
			{
				Text("", 150),
				SingBlock()
			});
			Block(new List<Grid>
			{
				Text("", 150),
				SingBlock()
			});
			Block(new List<Grid>
			{
				Text("Представитель грузоотправителя (поставщика, производителя)", 150),
				SingBlock()
			});
			Block(new List<Grid>
			{
				Text("Документ, удостоверяющий полномочия"),
				TextWithLineSign("","наименование")
			});
			Block(new List<Grid>
			{
				Text("№___________________ выдан «       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("Акт с приложением на _____________ листах получил"),
			});
			Block(new List<Grid>
			{
				Text("Главный (старший) бухгалтер", 300),
				TextWithLineSign("","подпись", 120),
				TextWithLineSign("","расшифровка подписи", 300)
			});
			Block(new List<Grid>
			{
				Text("«       »_____________             года"),
			});
			Block(new List<Grid>
			{
				Text("Решение руководителя"),
				TextWithLine("")
			});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid> {TextWithLine("")});
			Block(new List<Grid>
			{
				Text("Товар и тару на ответственное хранение принял"),
			});
			Block(new List<Grid>
			{
				Text("Заведующий складом (кладовщик)", 300),
				TextWithLineSign("","подпись", 120),
				TextWithLineSign("","расшифровка подписи", 300)
			});
		}

		private Grid LeftHeaderTable()
		{
			var grid = new Grid();
			grid.Cell(0, 0,
					SingBlockHeader(returnToSupplier.SupplierName,
						"организация, адрес, номер телефона, факса"))
				.Cell(1, 0, SingBlockHeader(waybillSettings.Address, "структурное подразделение"))
				.Cell(2, 0, SingBlockHeaderLabel("Основание для составления акта", "приказ, распоряжение"));
			grid.VerticalAlignment = VerticalAlignment.Center;
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			return grid;
		}
		private Grid RightHeaderTable()
		{
			var grid = new Grid();
			grid
				.Cell(0, 0, LabelWithoutBorder(""))
				.Cell(0, 1, LabelWithBorder("Код"))
				.Cell(1, 0, LabelWithoutBorder("Форма по ОКУД"))
				.Cell(1, 1, LabelWithBorder("330202"))
				.Cell(2, 0, LabelWithoutBorder("по ОКПО"))
				.Cell(2, 1, LabelWithBorder(""))
				.Cell(3, 0, LabelWithoutBorder(""))
				.Cell(3, 1, LabelWithBorder(""))
				.Cell(4, 0, LabelWithoutBorder(""))
				.Cell(4, 1, LabelWithBorder(""))
				.Cell(5, 0, LabelWithoutBorder("Вид деятельности по ОКДП"))
				.Cell(5, 1, LabelWithBorder(""))
				.Cell(6, 0, LabelWithoutBorder("номер"))
				.Cell(6, 1, LabelWithBorder(""))
				.Cell(7, 0, LabelWithoutBorder("дата"))
				.Cell(7, 1, LabelWithBorder(""))
				.Cell(8, 0, LabelWithoutBorder("Вид операции"))
				.Cell(8, 1, LabelWithBorder(""));
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
					TextAlignment = TextAlignment.Center,
					Width = 180
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

		private Grid Caption()
		{
			var grid = new Grid();
			grid.HorizontalAlignment = HorizontalAlignment.Center;
			grid
			.Cell(0, 0, CaptionTable())
			.Cell(1, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "об установленном расхождении по количеству",
						HorizontalAlignment = HorizontalAlignment.Center
					})
			.Cell(2, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "и качеству при приемке товарно-материальных ценностей",
						HorizontalAlignment = HorizontalAlignment.Center
					});
			return grid;
		}
		private Grid CaptionSignature()
		{
			var grid = new Grid();
			grid.Cell(0, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						Content = "УТВЕРЖДАЮ",
						HorizontalAlignment = HorizontalAlignment.Center
					})
				.Cell(1, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 12,
						FontWeight = FontWeights.Bold,
						Content = "Руководитель",
						HorizontalAlignment = HorizontalAlignment.Center
					})
				.Cell(2, 0, SingBlock("", "(должность)"))
				.Cell(3, 0, new Grid()
					.Cell(0, 0, SingBlock("", "    (подпись)     "))
					.Cell(0, 1, SingBlock("", "      (расшифровка подписи)      ")))
				.Cell(4, 0, new Grid()
					.Cell(0, 0, LabelWithoutBorder("<<______>>"))
					.Cell(0, 1, LabelWithoutBorder("__________ _____года"))
				);
			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}
		private Grid CaptionTable()
		{
			var grid = new Grid().Cell(1, 0, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 14,
					FontWeight = FontWeights.Bold,
					SnapsToDevicePixels = true,
					Content = "АКТ",
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
					Content = "В1-89"
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
