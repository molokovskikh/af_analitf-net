using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	[Description("Настройка печати реестра")]
	public class RegistryDocumentSettings
	{
		public RegistryDocumentSettings(Waybill waybill)
		{
			RegistryId = waybill.ProviderDocumentId;
			Date = waybill.DocumentDate;
		}

		[Display(Name = "Реестр №", Order = 0)]
		public string RegistryId { get; set; }

		[Display(Name = "Дата", Order = 1)]
		public DateTime Date { get; set; }

		[Display(Name = "Члены комиссии", Order = 2)]
		public string CommitteeMember1 { get; set; }

		[Display(Order = 3)]
		public string CommitteeMember2 { get; set; }

		[Display(Order = 4)]
		public string CommitteeMember3 { get; set; }
	}

	public class RegistryDocument : BaseDocument
	{
		private Waybill waybill;
		private WaybillSettings settings;
		private RegistryDocumentSettings docSettings;
		private IList<WaybillLine> lines;

		public RegistryDocument(Waybill waybill, IList<WaybillLine> lines, WaybillSettings settings, RegistryDocumentSettings docSettings)
		{
			this.waybill = waybill;
			this.docSettings = docSettings;
			this.settings = settings;
			this.lines = lines;

			doc.FontFamily = new FontFamily("Arial");
			BlockStyle = new Style(typeof(Paragraph)) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
					new Setter(System.Windows.Documents.Block.MarginProperty, new Thickness(0, 3, 0, 3))
				}
			};
			HeaderStyle = new Style(typeof(Run), HeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 12d),
					new Setter(Control.FontWeightProperty, FontWeights.Normal),
				}
			};
			TableHeaderStyle = new Style(typeof(TableCell), TableHeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
				}
			};
		}

		public override FlowDocument Build()
		{
			Landscape();

			var block = new Paragraph { Style = BlockStyle };
			block.Inlines.Add(new Figure(new Paragraph(new Run(
				"К положению о порядке формирования цен на лекарственные средства и изделия медицинского назначения\n"
				+ "Цены реестра соответствуют\n"
				+ "Гос. реестру 17 изд. 5 доп."))) {
					Width = new FigureLength(208),
					HorizontalAnchor = FigureHorizontalAnchor.PageLeft
				});
			//WrapDirection = WrapDirection.None нужно что бы блок с "УТВЕРЖДАЮ"
			//не пытался заполнить пустое пространство между этим блоком и предыдущим
			block.Inlines.Add(new Figure(new Paragraph(new Run(settings.FullName))) {
				HorizontalOffset = 713,
				HorizontalAnchor = FigureHorizontalAnchor.PageLeft,
				WrapDirection = WrapDirection.None
			});
			doc.Blocks.Add(block);

			block = new Paragraph { Style = BlockStyle };
			//WrapDirection = WrapDirection.None нужно что бы текст заголовка не пытался заполнить пустое пространстрво
			//а распологался отдельно
			block.Inlines.Add(new Figure(new Paragraph(new Run("УТВЕРЖДАЮ\n"
				+ String.Format("Зав.аптекой ______________{0}", settings.Director)))) {
					HorizontalAnchor = FigureHorizontalAnchor.PageRight,
					WrapDirection = WrapDirection.None
				});
			doc.Blocks.Add(block);

			var header = Header(String.Format("РЕЕСТР  №{0} от {1:d}\n"
				+ "розничных цен на лекарственные средства и изделия медицинского назначения,\n"
				+ "полученные от {4}-по счету (накладной) №{2} от {3:d}",
					docSettings.RegistryId,
					docSettings.Date,
					waybill.ProviderDocumentId,
					waybill.DocumentDate,
					waybill.Supplier == null ? "" : waybill.Supplier.Name));
			header.TextAlignment = TextAlignment.Center;

			var columns = new [] {
				new PrintColumnDeclaration("№ пп", 27),
				new PrintColumnDeclaration("Наименование и краткая характеристика товара", 200),
				new PrintColumnDeclaration("Серия товара", 84),
				new PrintColumnDeclaration("Срок годности", 60),
				new PrintColumnDeclaration("Наименование", 100),
				new PrintColumnDeclaration("Цена без НДС, руб", 56),
				new PrintColumnDeclaration("Цена с НДС, руб", 56),
				new PrintColumnDeclaration("Цена ГР, руб", 56),
				new PrintColumnDeclaration("Опт. надб. %", 32),
				new PrintColumnDeclaration("Отпуск. цена пост - ка без НДС, руб", 56),
				new PrintColumnDeclaration("НДС пост-ка, руб", 40),
				new PrintColumnDeclaration("Отпуск. цена пост-ка с НДС, руб", 56),
				new PrintColumnDeclaration("Розн. торг. надб. %", 33),
				new PrintColumnDeclaration("Розн. цена. за ед., руб", 56),
				new PrintColumnDeclaration("Кол-во", 36),
				new PrintColumnDeclaration("Розн. сумма, руб", 56),
			};
			var columnGrops = new [] {
				new ColumnGroup("Предприятие - изготовитель", 4, 6)
			};
			var rows = lines.Select((l, i) => new object[] {
				++i,
				l.Product,
				l.SerialNumber,
				l.Period,
				string.Format("{0} {1}", l.Producer, l.Country),
				l.ProducerCost,
				l.ProducerCostWithTax,
				l.RegistryCost,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.NdsAmount,
				l.SupplierCost,
				l.RetailMarkup,
				l.RetailCost,
				l.Quantity,
				l.RetailSum,
			});
				BuildTable(rows, columns, columnGrops);

			var retailsSum = lines.Sum(l => l.RetailSum);
			block = Block("Продажная сумма: " + RusCurrency.Str((double)retailsSum));
			block.Inlines.Add(new Figure(new Paragraph(new Run(retailsSum.ToString()))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});
			var sum = lines.Sum(l => l.Amount);
			block = Block("Сумма поставки: " + RusCurrency.Str((double)sum));
			block.Inlines.Add(new Figure(new Paragraph(new Run(sum.ToString()))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});
			Block("Члены комиссии:\n"
				+ String.Format("_{0}____/             /\n", docSettings.CommitteeMember1)
				+ String.Format("_{0}____/             /\n", docSettings.CommitteeMember2)
				+ String.Format("_{0}____/             /", docSettings.CommitteeMember3));

			return doc;
		}

		public override FrameworkContentElement GetHeader(int page, int pageCount)
		{
			return null;
		}

		public override FrameworkContentElement GetFooter(int page, int pageCount)
		{
			return new Paragraph(new Run(string.Format("страница {0} из {1}", page + 1, pageCount))) {
				FontFamily = new FontFamily("Arial"),
				FontSize = 8
			};
		}
	}
}