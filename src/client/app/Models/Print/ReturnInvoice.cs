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
	public class ReturnInvoice : BaseDocument
	{
		private DocumentTemplate template;
		private ReturnToSupplier returnToSupplier;
		private WaybillSettings waybillSettings;
		private BlockUIContainer bodyBlock;
		public ReturnInvoice(ReturnToSupplier returnToSupplier , WaybillSettings waybillSettings)
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
			var right = Block("Приложение N 1 \n" +
				"к постановлению Правительства Российской Федерации \n" +
				"от 26 декабря 2011 г. N 1137");
			right.TextAlignment = TextAlignment.Right;
			right.FontSize = 8;

			var name =
				Block("Счет-фактура № {0} от {1} \n".Format(returnToSupplier.Id, returnToSupplier.Date.ToString("dd/M/yyyy")));
			name.FontSize = 12;
			name.FontWeight = FontWeights.Bold;

			var body = Block("ИСПРАВЛЕНИЕ №                         от \n");
			body.FontSize = 8;
			BodyLine("Продавец", returnToSupplier == null ? "" : returnToSupplier.SupplierName);
			BodyLine("Адрес продавца", returnToSupplier == null ? "" : returnToSupplier.AddressName);
			BodyLine("ИНН/КПП", "");
			BodyLine("Грузоотправитель и его адрес",
				waybillSettings == null ? "" : waybillSettings.FullName + ", " + waybillSettings.Address);
			BodyLine("Грузополучатель и его адрес",
				returnToSupplier == null ? "" : returnToSupplier.SupplierName + ", " + returnToSupplier.AddressName);
			BodyLine("К платежно-расчетному документу №_______________ от _______________", "");
			BodyLine("Покупатель", waybillSettings == null ? "" : waybillSettings.FullName);
			BodyLine("Адрес покупателя", waybillSettings == null ? "" : waybillSettings.Address);
			BodyLine("ИНН/КПП покупателя", "");
			BodyLine("Валюта: наименование, код", "российский рубль, код 643");


			var columns = new[] {
				new PrintColumn("Наименование товара (описание выполненных работ, оказанных услуг), имущественного права", 244),
				new PrintColumn("код", 20),
				new PrintColumn("условное обозначение (национальное)", 84),
				new PrintColumn("Коли-чество (объем )", 44),
				new PrintColumn("Цена (тариф) за ед. изм.", 61),
				new PrintColumn("Стоимость товаров (работ, услуг), имуществен-ных прав без налога - всего", 77),
				new PrintColumn("В том числе сумма акциза", 39),
				new PrintColumn("Нало-говая став-ка", 39),
				new PrintColumn("Сумма налога, предъяв-ляемая покупате-лю", 55),
				new PrintColumn("Стоимость товаров (работ, услуг), имуществен-ных прав с налогом -всего", 77),
				new PrintColumn("цифровой код", 62),
				new PrintColumn("краткое наименование", 90),
				new PrintColumn("Номер грузовой таможенной декларации", 106)
			};
			var dataTable = BuildTableHeader(columns, new [] {
				new ColumnGroup("Единица измерения", 1, 2),
				new ColumnGroup("Страна происхождения товара", 10, 11),
			});
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);

			var header = new TableRow();
				(new [] { "1", "2", "2а", "3", "4", "5", "6", "7", "8", "9", "10", "10а", "11" })
				.Each(i => {
					var tableCell = Cell(i);
					tableCell.TextAlignment = TextAlignment.Center;
					tableCell.FontSize = 8;
					header.Cells.Add(tableCell);
				});

			dataTable.RowGroups[0].Rows.Add(header);

			var groups = returnToSupplier.Lines.GroupBy(l => l.Stock.Nds).OrderBy(g => g.Key);
			foreach (var taxGroup in groups) {
				var rows = taxGroup.OrderBy(l => l.Product).Select(l => new object[] {
					l.Product,
					l.ProductId,
					l.Stock.Unit,
					l.Quantity,
					l.SupplierCostWithoutNds.FormatCost(),
					"без",
					l.ExciseTax,
					$"{l.Stock.Nds}%",
					l.Stock.NdsAmount.FormatCost(),
					l.Stock.SupplySum,
					l.Stock.CountryCode,
					l.Stock.Country,
					l.BillOfEntryNumber
				});
				BuildRows(rows, columns, dataTable);
			}

			var result = new TableRow();
			result.FontWeight = FontWeights.Bold;
			result.Cells.Add(Cell("Всего к оплате", 5));
			result.Cells.Add(Cell(returnToSupplier.Lines.Sum(l => l.Stock.SupplySumWithoutNds)));
			result.Cells.Add(Cell(returnToSupplier.Lines.Sum(l => l.Stock.NdsAmount).FormatCost(), 3));
			result.Cells.Add(Cell(returnToSupplier.Lines.Sum(l => l.Stock.SupplySum)));
			dataTable.RowGroups[0].Rows.Add(result);
			doc.Blocks.Add(dataTable);

			var tax10Sum = returnToSupplier.Lines.Where(l => l.Stock.Nds == 10).Select(l => l.Stock.NdsAmount).Sum();
			var tax10Block = Block(string.Format("Итого НДС 10%: {0:0.00} руб", tax10Sum));
			tax10Block.FontWeight = FontWeights.Bold;

			var tax18Sum = returnToSupplier.Lines.Where(l => l.Stock.Nds == 18).Select(l => l.Stock.NdsAmount).Sum();
			var tax18Block = Block(string.Format("Итого НДС 18%: {0:0.00} руб", tax18Sum));
			tax18Block.FontWeight = FontWeights.Bold;

			doc.Blocks.Add(new BlockUIContainer(new Grid()
				.Cell(0, 0, SingBlock("Руководитель организации\r\nили иное уполномоченное лицо"))
				.Cell(1, 0, SingBlock("Индивидуальный предприниматель"))
				.RowSpan(0, 1, 2, new Label
				{
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Content = "М. П.",
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				})
				.Cell(0, 2, SingBlock("Главный бухгалтер\r\nили иное уполномоченное лицо"))
				.Cell(1, 2, new Grid()
					.Cell(0, 0, new Label
					{
						Width = 430,
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(0, 0, 0, 1),
						SnapsToDevicePixels = true,
						Margin = new Thickness(5, 0, 5, 0),
					})
					.Cell(1, 0, new Label
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 9,
						Content = new TextBlock
						{
							Text = "(реквизиты свидетельства о государственной регистрации индивидуального предпринимателя)",
							TextAlignment = TextAlignment.Center,
							TextWrapping = TextWrapping.Wrap,
						},
						HorizontalAlignment = HorizontalAlignment.Center,
					}))
				));
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

			grid.RowSpan(0, 0, 2, new Label {
				Content = new TextBlock {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Text = name,
					TextWrapping = TextWrapping.Wrap
				},
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
				Width = 145,
				BorderBrush = Brushes.Black,
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

		private void BodyLine(string label, string value)
		{
			if (bodyBlock == null) {
				bodyBlock = new BlockUIContainer();
				bodyBlock.Child = new Grid {
					HorizontalAlignment = HorizontalAlignment.Left,
					Margin = new Thickness(0, 10, 0, 10),
					Width = 820,
					ColumnDefinitions = {
						new ColumnDefinition(),
						new ColumnDefinition {
							Width = GridLength.Auto
						}
					}
				};
				doc.Blocks.Add(bodyBlock);
			}
			var grid = (Grid)bodyBlock.Child;
			grid.RowDefinitions.Add(new RowDefinition());
			var inner = new Grid();
			inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			inner.ColumnDefinitions.Add(new ColumnDefinition());
			var labelEl = new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = label,
			};
			labelEl.SetValue(Grid.ColumnProperty, 0);
			var valueEl = new Label {
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = value,
			};
			valueEl.SetValue(Grid.ColumnProperty, 1);
			inner.Children.Add(labelEl);
			inner.Children.Add(valueEl);
			inner.SetValue(Grid.ColumnProperty, 0);
			inner.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
			grid.Children.Add(inner);
		}

	}
}
