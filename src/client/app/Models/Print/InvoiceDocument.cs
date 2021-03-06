﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NPOI.POIFS.Storage;

namespace AnalitF.Net.Client.Models.Print
{
	public class InvoiceDocument : BaseDocument
	{
		private Waybill waybill;
		private BlockUIContainer headerBlock;

		public InvoiceDocument(Waybill waybill)
		{
			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			this.waybill = waybill;
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

		public override FrameworkContentElement GetHeader(int page, int pageCount)
		{
			if (page == 0)
				return null;

			return new Paragraph(new Run(string.Format("Счет-фактура {0} от {1:d}", waybill.ProviderDocumentId, waybill.DocumentDate))) {
				FontFamily = new FontFamily("Arial"),
				FontSize = 8
			};
		}

		public override FrameworkContentElement GetFooter(int page, int pageCount)
		{
			return new Paragraph(new Run($"страница {page + 1} из {pageCount}, время печати {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}")) {
				FontFamily = new FontFamily("Arial"),
				FontSize = 8
			};
		}

		protected override void BuildDoc()
		{
			var headerTable = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				Children = {
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = string.Format("СЧЕТ-ФАКТУРА № {0} от {1:d}", waybill.InvoiceId, waybill.InvoiceDate)
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "(1)"
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "ИСПРАВЛЕНИЕ № __________ от \" ___ \" ______________"
					},
					new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 14,
						FontWeight = FontWeights.Bold,
						Content = "(1а)",
					}
				}
			};
			headerTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			headerTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			headerTable.RowDefinitions.Add(new RowDefinition());
			headerTable.RowDefinitions.Add(new RowDefinition());

			headerTable.Children[0].SetValue(Grid.RowProperty, 0);
			headerTable.Children[0].SetValue(Grid.ColumnProperty, 0);

			headerTable.Children[1].SetValue(Grid.RowProperty, 0);
			headerTable.Children[1].SetValue(Grid.ColumnProperty, 1);

			headerTable.Children[2].SetValue(Grid.RowProperty, 1);
			headerTable.Children[2].SetValue(Grid.ColumnProperty, 0);

			headerTable.Children[3].SetValue(Grid.RowProperty, 1);
			headerTable.Children[3].SetValue(Grid.ColumnProperty, 1);

			doc.Blocks.Add(new BlockUIContainer(headerTable));

			HeaderLine("Продавец", waybill.Seller == null ? "" : waybill.Seller.Name, "2");
			HeaderLine("Адрес продавца", waybill.Seller == null ? "" : waybill.Seller.Address, "2а");
			HeaderLine("ИНН/КПП", String.Format("{0}/{1}", waybill.Seller == null ? "" : waybill.Seller.Inn, waybill.Seller == null ? "" : waybill.Seller.Kpp), "2б");
			HeaderLine("Грузоотправитель и его адрес", waybill.ShipperNameAndAddress, "3");
			HeaderLine("Грузополучатель и его адрес", waybill.ConsigneeNameAndAddress, "4");
			HeaderLine("К платежно-расчетному документу №_______________ от _______________", "", "5");
			HeaderLine("Покупатель", waybill.Buyer == null ? "" : waybill.Buyer.Name, "6");
			HeaderLine("Адрес покупателя", waybill.Buyer == null ? "" : waybill.Buyer.Address, "6а");
			HeaderLine("ИНН/КПП покупателя", String.Format("{0}/{1}", waybill.Buyer == null ? "" : waybill.Buyer.Inn, waybill.Buyer == null ? "" : waybill.Buyer.Kpp), "6б");
			HeaderLine("Валюта: наименование, код", "российский рубль, код 643", "7");

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

			var groups = waybill.Lines.GroupBy(l => l.Nds).OrderBy(g => g.Key);
			foreach (var taxGroup in groups) {
				var rows = taxGroup.OrderBy(l => l.Product).Select(l => new object[] {
					l.Product,
					null,
					l.Unit,
					l.Quantity,
					l.SupplierCostWithoutNds.FormatCost(),
					l.AmountExcludeTax.FormatCost(),
					l.ExciseTax,
					$"{l.Nds}%",
					l.NdsAmount.FormatCost(),
					l.Amount.FormatCost(),
					l.CountryCode,
					l.Country,
					l.BillOfEntryNumber
				});
				BuildRows(rows, columns, dataTable);
				var row = new TableRow();
				row.FontWeight = FontWeights.Bold;
				row.Cells.Add(Cell("Итого", 5));
				row.Cells.Add(Cell(taxGroup.Sum(l => l.AmountExcludeTax).FormatCost()));
				row.Cells.Add(Cell(taxGroup.Sum(l => l.NdsAmount).FormatCost(), 3));
				row.Cells.Add(Cell(taxGroup.Sum(l => l.Amount).FormatCost()));
				row.Cells.Add(Cell("", 3));
				dataTable.RowGroups[0].Rows.Add(row);
			}

			var result = new TableRow();
			result.FontWeight = FontWeights.Bold;
			result.Cells.Add(Cell("Всего к оплате", 5));
			result.Cells.Add(Cell((waybill.DisplayedSum - waybill.DisplayedTaxSum).ToString("0.00")));
			result.Cells.Add(Cell(waybill.DisplayedTaxSum.ToString("0.00"), 3));
			result.Cells.Add(Cell(waybill.DisplayedSum.ToString("0.00")));
			dataTable.RowGroups[0].Rows.Add(result);
			doc.Blocks.Add(dataTable);

			var tax10Sum = waybill.Lines.Where(l => l.Nds == 10).Select(l => l.NdsAmount).Sum();
			var tax10Block = Block(string.Format("Итого НДС 10%: {0:0.00} руб", tax10Sum));
			tax10Block.FontWeight = FontWeights.Bold;

			var tax18Sum = waybill.Lines.Where(l => l.Nds == 18).Select(l => l.NdsAmount).Sum();
			var tax18Block = Block(string.Format("Итого НДС 18%: {0:0.00} руб", tax18Sum));
			tax18Block.FontWeight = FontWeights.Bold;

			doc.Blocks.Add(new BlockUIContainer(new Grid()
				.Cell(0, 0, SingBlock("Руководитель организации\r\nили иное уполномоченное лицо"))
				.Cell(1, 0, SingBlock("Индивидуальный предприниматель"))
				.RowSpan(0, 1, 2, new Label {
					FontFamily = new FontFamily("Arial"),
					FontSize = 12,
					Content = "М. П.",
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				})
				.Cell(0, 2, SingBlock("Главный бухгалтер\r\nили иное уполномоченное лицо"))
				.Cell(1, 2, new Grid()
					.Cell(0, 0, new Label {
						Width = 430,
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(0, 0, 0, 1),
						SnapsToDevicePixels = true,
					})
					.Cell(1, 0, new Label {
						FontFamily = new FontFamily("Arial"),
						FontSize = 9,
						Content = new TextBlock {
							Text = "(реквизиты свидетельства о государственной регистрации\r\nиндивидуального предпринимателя)",
							TextAlignment = TextAlignment.Center,
							TextWrapping = TextWrapping.Wrap
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

		private void HeaderLine(string label, string value, string id)
		{
			if (headerBlock == null) {
				headerBlock = new BlockUIContainer();
				headerBlock.Child = new Grid {
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
				doc.Blocks.Add(headerBlock);
			}
			var grid = (Grid)headerBlock.Child;
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
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 0, 0, 1),
				SnapsToDevicePixels = true,
				Content = value,
			};
			valueEl.SetValue(Grid.ColumnProperty, 1);
			inner.Children.Add(labelEl);
			inner.Children.Add(valueEl);
			inner.SetValue(Grid.ColumnProperty, 0);
			inner.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
			grid.Children.Add(inner);
			var idEl = new Label {
				Content = "(" + id + ")",
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
			};
			idEl.SetValue(Grid.ColumnProperty, 1);
			idEl.SetValue(Grid.RowProperty, grid.RowDefinitions.Count - 1);
			grid.Children.Add(idEl);
		}
	}
}