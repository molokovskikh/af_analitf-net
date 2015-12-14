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
	[Description("Настройка печати накладной")]
	public class WaybillDocumentSettings
	{
		public WaybillDocumentSettings Setup(Waybill waybill)
		{
			ProviderDocumentId = waybill.ProviderDocumentId;
			DocumentDate = waybill.DocumentDate;
			return this;
		}

		[Display(Name = "Накладная №", Order = 0), Ignore]
		public string ProviderDocumentId { get; set; }

		[Display(Name = "Дата", Order = 1), Ignore]
		public DateTime DocumentDate { get; set; }

		[Display(Name = "Через кого", Order = 2)]
		public string OperatedBy { get; set; }

		[Display(Name = "Затребовал", Order = 3)]
		public string ReqestedBy { get; set; }

		[Display(Name = "Отпустил", Order = 4)]
		public string SentBy { get; set; }

		[Display(Name = "Получил", Order = 5)]
		public string GotBy { get; set; }
	}

	public class WaybillDocument : BaseDocument
	{
		private Waybill waybill;
		private WaybillSettings settings;
		private DocumentTemplate template;
		private IList<WaybillLine> lines;
		private WaybillDocumentSettings docSettings;

		public WaybillDocument(Waybill waybill, IList<WaybillLine> lines)
		{
			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			this.waybill = waybill;
			this.settings = waybill.WaybillSettings;
			this.lines = lines;
			docSettings = waybill.GetWaybillDocSettings();
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
			Header($"\n                               Наименование организации: {settings.FullName}\n"
				+ "             Отдел:______________________________________________\n");

			TwoColumns();
			var left = Header("Требование №________________________\n"
				+ "от \"_____\" _______________20__г\n"
				+ "кому: ___________________________________\n"
				+ "Основание отпуска________________________\n");
			left.TextAlignment = TextAlignment.Center;
			Header($"Накладная № {docSettings.ProviderDocumentId} {waybill.SupplierName}\n"
				+ $"от  {docSettings.DocumentDate:d}\n"
				+ $"Через кого   {docSettings.OperatedBy}\n"
				+ "Доверенность № _______от \"______\" ________20__г\n");

			var columns = new[] {
				new PrintColumn("№ пп", 27),
				new PrintColumn("Наименование и краткая характеристика товара", 170, colSpan: 2),
				new PrintColumn("", 30),
				new PrintColumn("Серия товара", 50),
				new PrintColumn("Сертификат", 66),
				new PrintColumn("Срок годности", 60),
				new PrintColumn("Наименование", 124),
				new PrintColumn("Цена без НДС, руб", 56),
				new PrintColumn("Затребован.колич.", 56),
				new PrintColumn("Опт. надб. %", 32),
				new PrintColumn("Отпуск. цена пост-ка без НДС, руб", 56),
				new PrintColumn("НДС пост-ка, руб", 40),
				new PrintColumn("Отпуск. цена пост-ка с НДС, руб", 56),
				new PrintColumn("Розн. торг. надб. %", 33),
				new PrintColumn("Розн. цена. за ед., руб", 56),
				new PrintColumn("Кол-во", 36),
				new PrintColumn("Розн. сумма, руб", 56),
			};
			var columnGrops = new[] {
				new ColumnGroup("Предприятие - изготовитель", 5, 6)
			};
			var rows = lines.Select((l, i) => new object[] {
				++i,
				l.Product,
				l.ActualVitallyImportant ? "ЖВ" : "",
				l.SerialNumber,
				l.Certificates,
				l.Period,
				$"{l.Producer} {l.Country}",
				l.ProducerCost,
				l.Quantity,
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds,
				l.SupplierCost - l.SupplierCostWithoutNds,
				l.SupplierCost,
				l.RetailMarkup,
				l.RetailCost,
				l.Quantity,
				l.RetailSum
			});
			BuildTable(rows, columns, columnGrops);

			var retailSum = lines.Sum(l => l.RetailSum);
			var block = Block("Продажная сумма: " + RusCurrency.Str((double)retailSum));
			block.Inlines.Add(new Figure(new Paragraph(new Run(retailSum.ToString()))) {
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

			TwoColumns();
			Block($"Затребовал:  {docSettings.ReqestedBy}\n\n"
				+ "место печати       подпись     _________________\n\n"
				+ "\" ____\" _______________20__г\n");
			Block($"Отпустил: Сдал (выдал)________________{docSettings.SentBy}\n\n"
				+ $"Получил:Принял(получил)______________{docSettings.GotBy}\n\n"
				+ $"Руководитель учреждения_____________{settings.Director}\n\n"
				+ $"Главный (старший)бухгалтер ________________{settings.Accountant}\n");
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