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
	[Description("Настройка печати протокола согласования цен")]
	public class WaybillProtocolDocumentSettings
	{
		public WaybillProtocolDocumentSettings Setup(Waybill waybill)
		{
			ProviderDocumentId = waybill.ProviderDocumentId;
			DocumentDate = waybill.DocumentDate;
			return this;
		}

		[Display(Name = "Протокол №", Order = 0), Ignore]
		public string ProviderDocumentId { get; set; }

		[Display(Name = "Дата", Order = 1), Ignore]
		public DateTime DocumentDate { get; set; }

		[Display(Name = "Уполномоченное лицо", Order = 3)]
		public string Person1Name { get; set; }
	}

	public class WaybillProtocolDocument : BaseDocument
	{
		private Waybill waybill;
		private WaybillSettings settings;
		private DocumentTemplate template;
		private IList<WaybillLine> lines;
		private WaybillProtocolDocumentSettings docSettings;

		public WaybillProtocolDocument(Waybill waybill, IList<WaybillLine> lines)
		{
			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			this.waybill = waybill;
			this.settings = waybill.WaybillSettings;
			this.lines = lines;
			docSettings = waybill.GetWaybillProtocolDocSettings();
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
			doc.Blocks.Add(
				new Paragraph(
					new Run("Утверждено ПП РФ от 29.10.10 №865\n(с изм. в ред. ПП РФ от 03.02.16 №58)") {
						FontSize = 8d,
					}) {
					TextAlignment = TextAlignment.Right,
				});

			var h1 = Header($"ПРОТОКОЛ\nсогласования цен поставки лекарственных препаратов,\nвключенных в перечень жизненно необходимых и важнейших лекарственных препаратов\nпо документу {docSettings.ProviderDocumentId} от {docSettings.DocumentDate:d}");
			var h2 = Header($"({waybill.ProviderDocumentId} от {waybill.DocumentDate:d})");
			h1.TextAlignment = h2.TextAlignment = TextAlignment.Center;
			((Run)h1.Inlines.FirstInline).FontWeight = FontWeights.Bold;

			var sh1 = new BlockUIContainer(StrokeBlock(waybill.SupplierName, "поставщик"));
			var sh2 = new BlockUIContainer(StrokeBlock(settings.FullName, "получатель (организация оптовой торговли или организации розничной торговли)"));
			doc.Blocks.Add(sh1);
			doc.Blocks.Add(sh2);

			var columns = new[] {
				new PrintColumn("Торговое наименование, лекарственная форма, дозировка, количество в потребительской упаковке", 270),
				new PrintColumn("Серия", 60),
				new PrintColumn("Производитель", 110),
				new PrintColumn("Зарегистрированная предельная отпускная цена, установленная производителем (рублей)", 60),
				new PrintColumn("Фактическая отпускная цена, установленная производителем, без НДС (рублей)", 60),
				new PrintColumn("проц.", 33),
				new PrintColumn("руб.", 33),
				new PrintColumn("Фактическая отпускная цена, установленная организацией оптовой торговли, без НДС (рублей)", 60),
				new PrintColumn("проц.", 33),
				new PrintColumn("руб.", 33),
				new PrintColumn("Фактическая отпускная цена, установленная организацией оптовой торговли, без НДС (рублей)", 60),
				new PrintColumn("проц.", 33),
				new PrintColumn("руб.", 33),

				new PrintColumn("проц.", 33),
				new PrintColumn("руб.", 33),
				new PrintColumn("Фактическая отпускная цена, установленная организацией розничной торговли, без НДС (рублей)", 60),
			};
			var columnGrops = new[] {
				new ColumnGroup("Размер фактической оптовой надбавки, установленной организацией оптовой торговли", 5, 6),
				new ColumnGroup("Размер фактической оптовой надбавки, установленной организацией оптовой торговли", 8, 9),
				new ColumnGroup("Суммарный размер фактических оптовых надбавок, установленных организациями розничной торговли", 11, 12),
				new ColumnGroup("Размер фактической розничной надбавки, установленной организацией розничной торговли", 13, 14),
			};
			var rows = lines.Select((l, i) => new object[] {
				l.Product,
				l.SerialNumber,
				l.Producer,
				l.RegistryCost,
				l.ProducerCost,
				"",
				"",
				"",
				l.SupplierPriceMarkup.HasValue ? l.SupplierPriceMarkup.Value.ToString("0.00") : "",
				l.SupplierPriceMarkup.HasValue && l.SupplierCostWithoutNds.HasValue ? (l.SupplierPriceMarkup.Value * l.SupplierCostWithoutNds.Value / 100).ToString("0.00") : "",
				l.SupplierCostWithoutNds,
				"",
				"",
				l.RetailMarkup.HasValue ? l.RetailMarkup.Value.ToString("0.00") : "",
				l.RetailMarkupInRubles.HasValue ? l.RetailMarkupInRubles.Value.ToString("0.00") : "",
				l.RetailCostWithoutNds.HasValue ? l.RetailCostWithoutNds.Value.ToString("0.00") : "",
			});

			var table = BuildTableHeader(columns, columnGrops);
			var row = new TableRow();
			for (int i = 1; i <= columns.Length; i++) {
				row.Cells.Add(new TableCell(new Paragraph(new Run(i.ToString())))
				{
					Style = TableHeaderStyle
				});
			}
			table.RowGroups[0].Rows.Add(row);
			BuildRows(rows, columns, table);
			doc.Blocks.Add(table);
			doc.Blocks.Add(new BlockUIContainer(Caption()));
		}

		private Grid Caption()
		{
			var grid = new Grid();
			grid.Cell(0, 0, new Label())
				.Cell(1, 0, new Label{
					FontSize = 9,
					Content = "(подпись уполномоченного лица поставщика - организации\nоптовой торговли или организации розничной торговли -\nуказать нужное)",
					HorizontalContentAlignment = HorizontalAlignment.Center,
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(0, 1, 0, 0),
					SnapsToDevicePixels = true,
				})
				.Cell(2, 0, new Label{
					FontSize = 12,
					Content = docSettings.DocumentDate.ToString("d"),
					HorizontalContentAlignment = HorizontalAlignment.Center,
				})
				.Cell(3, 0, new Label{
					FontSize = 9,
					Content = "М.П.",
					HorizontalContentAlignment = HorizontalAlignment.Center,
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(0, 1, 0, 0),
					SnapsToDevicePixels = true,
				})
				.Cell(0, 1, new Label{
					FontSize = 12,
					Content = docSettings.Person1Name,
					HorizontalContentAlignment = HorizontalAlignment.Center,
				})
				.Cell(1, 1, new Label{
					FontSize = 9,
					Content = "(ф.и.о.)",
					HorizontalContentAlignment = HorizontalAlignment.Center,
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(0, 1, 0, 0),
					SnapsToDevicePixels = true,
					Width = 200
				})
				.Cell(2, 1, new Label())
				.Cell(3, 1, new Label());

			grid.HorizontalAlignment = HorizontalAlignment.Right;
			return grid;
		}

		private Grid StrokeBlock(string text, string signature)
		{
			var grid = new Grid() {
				Width = 1000
			};
			grid.RowDefinitions.Add(new RowDefinition());
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition());

			grid.Cell(0, 0, new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 12,
				Content = text,
				HorizontalAlignment = HorizontalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				Width = 600,
			});
			grid.Cell(1, 0, new Label
			{
				FontFamily = new FontFamily("Arial"),
				FontSize = 9,
				Content = signature,
				HorizontalAlignment = HorizontalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 1, 0, 0),
				SnapsToDevicePixels = true,
				Width = 600,
			});
			return grid;
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