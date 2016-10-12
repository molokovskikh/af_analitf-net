using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Config.NHibernate;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	[Description("Настройка печати акта прихода")]
	public class WaybillActDocumentSettings
	{
		public WaybillActDocumentSettings Setup(Waybill waybill)
		{
			ProviderDocumentId = waybill.ProviderDocumentId;
			DocumentDate = waybill.DocumentDate;
			Person1Position = "Фармацевт";
			Person2Position = "Зав. аптекой";
			Person3Position = "Директор";
			Person3Name = waybill.WaybillSettings.Director;
			return this;
		}

		[Display(Name = "Акт №", Order = 0), Ignore]
		public string ProviderDocumentId { get; set; }

		[Display(Name = "Дата", Order = 1), Ignore]
		public DateTime DocumentDate { get; set; }

		[Display(Name = "Принял, должность", Order = 2)]
		public string Person1Position { get; set; }

		[Display(Name = "Принял, ФИО", Order = 3)]
		public string Person1Name { get; set; }

		[Display(Name = "Товар получен полностью, должность", Order = 4)]
		public string Person2Position { get; set; }

		[Display(Name = "Товар получен полностью, ФИО", Order = 5)]
		public string Person2Name { get; set; }

		[Display(Name = "Кол-во и цены проверены, должность", Order = 6)]
		public string Person3Position { get; set; }

		[Display(Name = "Кол-во и цены проверены, ФИО", Order = 7)]
		public string Person3Name { get; set; }
	}

	public class WaybillActDocument : BaseDocument
	{
		private Waybill waybill;
		private WaybillSettings settings;
		private DocumentTemplate template;
		private IList<WaybillLine> lines;
		private WaybillActDocumentSettings docSettings;

		public WaybillActDocument(Waybill waybill, IList<WaybillLine> lines)
		{
			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			this.waybill = waybill;
			this.settings = waybill.WaybillSettings;
			this.lines = lines;
			docSettings = waybill.GetWaybillActDocSettings();
			Settings = docSettings;

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
					new Setter(System.Windows.Documents.Block.MarginProperty, new Thickness(0, 0, 0, 0))
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
			var h1 = Header($"Акт прихода товара по документу {docSettings.ProviderDocumentId} от {docSettings.DocumentDate:d}");
			var h2 = Header($"по счет-фактуре {waybill.ProviderDocumentId} от {waybill.DocumentDate:d}\nпоставщик: {waybill.SupplierName}");
			h1.TextAlignment = h2.TextAlignment = TextAlignment.Center;
			((Run)h1.Inlines.FirstInline).FontWeight  = FontWeights.Bold;

			var columns = new[] {
				new PrintColumn("№ пп", 28),
				new PrintColumn("Наименование", 356),
				new PrintColumn("Кол-во", 34),
				new PrintColumn("Серия\nСрок", 63),
				new PrintColumn("ЖВЛ", 16),
				new PrintColumn("Заводской штрихкод", 75),
				new PrintColumn("НДС", 16),
				new PrintColumn("Цена", 59),
				new PrintColumn("Сумма", 59),
				new PrintColumn("Цена", 59),
				new PrintColumn("Сумма", 59),
				new PrintColumn("Сертификат", 180),
			};
			var columnGrops = new[] {
				new ColumnGroup("Оптовое звено", 7, 8),
				new ColumnGroup("Розничное", 9, 10)
			};
			var rows = lines.Select((l, i) => new object[] {
				++i,
				$"{l.Product}\n{l.Producer} {l.Country}",
				l.Quantity,
				$"{l.SerialNumber}\n{l.Period}",
				l.ActualVitallyImportant ? "+" : "",
				l.EAN13,
				l.Nds,
				l.SupplierCost,
				l.Amount,
				l.RetailCost,
				l.RetailSum,
				$"{l.Certificates}",
			});
			var table = BuildTable(rows, columns, columnGrops);
			var retailSum = lines.Sum(l => l.RetailSum);
			var sum = lines.Sum(l => l.Amount);

			table.RowGroups[0].Rows.Add(new TableRow
			{
				Cells = {
					new TableCell(new Paragraph(new Run("Итого: ")) {
						KeepTogether = true
					}) {
						Style = CellStyle,
						ColumnSpan = 7
					},
					new TableCell(new Paragraph(new Run(sum.ToString())) {
						KeepTogether = true
					}) {
						Style = CellStyle,
						ColumnSpan = 2,
						FontWeight = FontWeights.Bold,
						TextAlignment = TextAlignment.Right
					},
					new TableCell(new Paragraph(new Run(retailSum.ToString())) {
						KeepTogether = true
					}) {
						Style = CellStyle,
						ColumnSpan = 2,
						FontWeight = FontWeights.Bold,
						TextAlignment = TextAlignment.Right
					},
					new TableCell(new Paragraph(new Run()) {
						KeepTogether = true
					}) {
						Style = CellStyle,
					}
				}
			});

			TwoColumns();

			var table2 = new Table() {
				FontSize = 10d,
				RowGroups = { new TableRowGroup() {
					Rows = {
						new TableRow() {
							Cells = {
								new TableCell(new Paragraph(new Run("По ценам закупочным:"))),
								new TableCell(new Paragraph(new Run(RusCurrency.Str((double)sum)))),
							}
						},
						new TableRow() {
							Cells = {
								new TableCell(new Paragraph(new Run("По ценам розничным:"))),
								new TableCell(new Paragraph(new Run(RusCurrency.Str((double)retailSum)))),
							}
						},
						new TableRow() {
							Cells = {
								new TableCell(new Paragraph(new Run("Сумма розничной наценки:"))),
								new TableCell(new Paragraph(new Run(RusCurrency.Str((double)(retailSum - sum))))),
							}
						},
					}
				}}
			};
			Stash(table2);

			var table3 = new Table()
			{
				FontSize = 10d,
				RowGroups = { new TableRowGroup() {
					Rows = {
						new TableRow() {
							Cells = {
								new TableCell(new Paragraph(new Run("Принял"))),
								new TableCell(new Paragraph(new Run($"{docSettings.Person1Position} {docSettings.Person1Name}"))),
								new TableCell(new Paragraph(new Run("__________________"))),
							}
						},
						new TableRow() {
							Cells = {
								new TableCell(new Paragraph(new Run("Товар получен полностью"))),
								new TableCell(new Paragraph(new Run($"{docSettings.Person2Position} {docSettings.Person2Name}"))),
								new TableCell(new Paragraph(new Run("__________________"))),
							}
						},
						new TableRow() {
							Cells = {
								new TableCell(new Paragraph(new Run("Кол-во и цены проверены"))),
								new TableCell(new Paragraph(new Run($"{docSettings.Person3Position} {docSettings.Person3Name}"))),
								new TableCell(new Paragraph(new Run("__________________"))),
							}
						},
					}
				}}
			};
			Stash(table3);
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