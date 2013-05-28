﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	public class InvoiceDocument : BaseDocument
	{
		private Waybill waybill;

		public InvoiceDocument(Waybill waybill)
		{
			doc.FontFamily = new FontFamily("Arial");
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
			return new Paragraph(new Run(string.Format("страница {0} из {1}", page + 1, pageCount))) {
				FontFamily = new FontFamily("Arial"),
				FontSize = 8
			};
		}

		public override FlowDocument Build()
		{
			Landscape();

			Header(string.Format("Счет-фактура {0} от {1:d}", waybill.ProviderDocumentId, waybill.DocumentDate));
			Block("Продавец: ");
			Block("Адрес продавца: ");
			Block("ИНН/КПП: ");
			Block("Грузоотправитель и его адрес: ");
			Block("Грузополучатель и его адрес: ");
			Block("К платежно-расчетному документу №_______________ от _______________");
			Block("Покупатель: ");
			Block("Адрес покупателя: ");
			Block("ИНН/КПП покупателя: ");
			var headerTable = TwoColumns(GridLength.Auto, "", "Вылюта: россиский рубль");
			headerTable.CellSpacing = 0;
			headerTable.Margin = new Thickness(0, 0, 0, 0);
			doc.Blocks.Add(headerTable);
			var columns = new[] {
				new PrintColumnDeclaration("Наименование товара (описание выполненных работ, оказанных услуг)", 303),
				new PrintColumnDeclaration("Ед. изм.", 29),
				new PrintColumnDeclaration("Кол-во", 44),
				new PrintColumnDeclaration("Цена (тариф) за ед. изм.", 69),
				new PrintColumnDeclaration("Стоимость товаров (работ, услуг) всего без налога", 85),
				new PrintColumnDeclaration("В том числе акциз", 39),
				new PrintColumnDeclaration("Налоговая ставка", 39),
				new PrintColumnDeclaration("Сумма налога", 71),
				new PrintColumnDeclaration("Стоимость товаров (работ, услуг), всего с налогом", 85),
				new PrintColumnDeclaration("Страна происхождения", 99),
				new PrintColumnDeclaration("Номер грузовой таможенной декларации", 135)
			};
			var dataTable = BuildTableHeader(columns);
			dataTable.Margin = new Thickness(dataTable.Margin.Left, 0, dataTable.Margin.Right, dataTable.Margin.Bottom);
			var header = new TableRow();
			Enumerable.Range(1, columns.Length)
				.Each(i => {
					var tableCell = Cell(i.ToString());
					tableCell.TextAlignment = TextAlignment.Center;
					tableCell.FontSize = 8;
					header.Cells.Add(tableCell);
				});

			dataTable.RowGroups[0].Rows.Add(header);

			var groups = waybill.Lines.GroupBy(l => l.NDS);
			foreach (var taxGroup in groups) {
				var rows = taxGroup.Select(l => new object[] {
					l.Product,
					l.Unit,
					l.Quantity,
					l.SupplierCostWithoutNds,
					l.Amount - l.NDSAmount,
					l.ExciseTax,
					string.Format("{0}%", l.NDS),
					l.NDSAmount,
					l.Amount,
					l.Country,
					l.BillOfEntryNumber
				});
				BuildRows(rows, columns, dataTable);
				var row = new TableRow();
				row.FontWeight = FontWeights.Bold;
				row.Cells.Add(Cell("Итого", 4));
				row.Cells.Add(Cell(taxGroup.Sum(l => l.AmountExcludeTax)));
				row.Cells.Add(Cell(taxGroup.Sum(l => l.NDSAmount), 3));
				row.Cells.Add(Cell(taxGroup.Sum(l => l.Amount)));
				row.Cells.Add(Cell("", 2));
				dataTable.RowGroups[0].Rows.Add(row);
			}

			var result = new TableRow();
			result.FontWeight = FontWeights.Bold;
			result.Cells.Add(Cell("Всего к оплате", 4));
			result.Cells.Add(Cell(waybill.Lines.Sum(l => l.AmountExcludeTax)));
			result.Cells.Add(Cell(waybill.Lines.Sum(l => l.NDSAmount), 3));
			result.Cells.Add(Cell(waybill.Lines.Sum(l => l.Amount)));
			result.Cells.Add(Cell("", 2));
			dataTable.RowGroups[0].Rows.Add(result);
			doc.Blocks.Add(dataTable);

			var tax10Sum = waybill.Lines.Where(l => l.NDS == 10).Select(l => l.NDSAmount).Sum();
			var tax10Block = Block(string.Format("Итого НДС 10%: {0} руб", tax10Sum));
			tax10Block.FontWeight = FontWeights.Bold;

			var tax18Sum = waybill.Lines.Where(l => l.NDS == 18).Select(l => l.NDSAmount).Sum();
			var tax18Block = Block(string.Format("Итого НДС 18%: {0} руб", tax18Sum));
			tax18Block.FontWeight = FontWeights.Bold;

			var table = TwoColumns(GridLength.Auto, "Руководитель органзации _____________________", "Главный бухгалтер _____________________");
			table.Margin = new Thickness(0);
			doc.Blocks.Add(table);
			Block("(индивидуальный предприниматель)");
			table = TwoColumns(new GridLength(250), "", "М. П.");
			table.RowGroups[0].Rows[0].Cells[1].TextAlignment = TextAlignment.Left;
			doc.Blocks.Add(table);

			table = new Table {
				Columns = {
					new TableColumn {
						Width = new GridLength(130)
					},
					new TableColumn {
						Width = new GridLength(250)
					},
				},
				RowGroups = {
					new TableRowGroup {
						Rows = {
							new TableRow {
								Cells = {
									new TableCell(new Paragraph(new Run("ВЫДАЛ")) {
										Style = BlockStyle
									}),
									new TableCell(new Paragraph(new Run("_________________________________________")) {
										Style = BlockStyle
									}),
								}
							},
							new TableRow {
								Cells = {
									new TableCell(new Paragraph(new Run("")) {
										Style = BlockStyle,
									}),
									new TableCell(new Paragraph(new Run("(подпись ответственного лица от продавца)"))) {
										TextAlignment = TextAlignment.Center,
										FontSize = 7
									}
								}
							}
						}
					}
				}
			};
			doc.Blocks.Add(table);

			table = new Table {
				Columns = {
					new TableColumn {
						Width = new GridLength(75)
					},
					new TableColumn {
						Width = GridLength.Auto
					},
				},
				RowGroups = {
					new TableRowGroup {
						Rows = {
							new TableRow {
								Cells = {
									new TableCell(new Paragraph(new Run("ПРИМЕЧАНИЕ: ")) {
										FontSize = 7,
									}),
									new TableCell(new Paragraph(new Run("1. Без печати недействительно\n2. Первый экземпляр - покупателю, второй экземпляр - продавцу"))) {
										FontSize = 7,
									}
								}
							}
						}
					}
				}
			};
			table.RowGroups[0].Rows[0].Cells[1].TextAlignment = TextAlignment.Left;
			doc.Blocks.Add(table);
			return doc;
		}

		private Table TwoColumns(GridLength leftWidth, string left, string right)
		{
			var table = new Table {
				Columns = {
					new TableColumn {
						Width = leftWidth
					},
					new TableColumn {
						Width = GridLength.Auto
					},
				},
				RowGroups = {
					new TableRowGroup {
						Rows = {
							new TableRow {
								Cells = {
									new TableCell(new Paragraph(new Run(left)) { Style = BlockStyle }),
									new TableCell(new Paragraph(new Run(right)) { Style = BlockStyle }) {
										TextAlignment = TextAlignment.Right
									}
								}
							}
						}
					}
				}
			};
			return table;
		}
	}
}