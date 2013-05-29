﻿using System;
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
	public class DocumentTemplate
	{
		public List<FrameworkContentElement> Parts = new List<FrameworkContentElement>();

		public Block ToBlock()
		{
			var table = new Table {
				Columns = {
					new TableColumn {
						Width = GridLength.Auto
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
									new TableCell((Block)Parts[0]),
									new TableCell((Block)Parts[1])
								}
							}
						}
					}
				}
			};
			return table;
		}

		public bool IsReady
		{
			get { return Parts.Count == 2; }
		}
	}

	[Description("Настройка печати накладной")]
	public class WaybillDocumentSettings
	{
		public WaybillDocumentSettings(Waybill waybill)
		{
			ProviderDocumentId = waybill.ProviderDocumentId;
			DocumentDate = waybill.DocumentDate;
		}

		[Display(Name = "Накладная №", Order = 0)]
		public string ProviderDocumentId { get; set; }

		[Display(Name = "Дата", Order = 1)]
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
		private WaybillDocumentSettings docSettings;

		public WaybillDocument(Waybill waybill, WaybillSettings settings, WaybillDocumentSettings docSettings)
		{
			this.waybill = waybill;
			this.settings = settings;
			this.docSettings = docSettings;

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

			Header(String.Format("\n                               Наименование организации: {0}\n", settings.FullName)
				+ "             Отдел:______________________________________________\n");

			TwoColumns();
			var left = Header("Требование №________________________\n"
				+ "от \"_____\" _______________20__г\n"
				+ "кому: ___________________________________\n"
				+ "Основание отпуска________________________\n");
			left.TextAlignment = TextAlignment.Center;
			Header(String.Format("Накладная №  {0}\n", docSettings.ProviderDocumentId)
				+ String.Format("от  {0:d}\n", docSettings.DocumentDate)
				+ String.Format("Через кого   {0}\n", docSettings.OperatedBy)
				+ "Доверенность № _______от \"______\" ________20__г\n");

			var columns = new [] {
				new PrintColumnDeclaration("№ пп", 27),
				new PrintColumnDeclaration("Наименование и краткая характеристика товара", 200),
				new PrintColumnDeclaration("Серия товара", 50),
				new PrintColumnDeclaration("Сертификат", 66),
				new PrintColumnDeclaration("Срок годности", 60),
				new PrintColumnDeclaration("Наименование", 124),
				new PrintColumnDeclaration("Цена без НДС, руб", 56),
				new PrintColumnDeclaration("Затребован.колич.", 56),
				new PrintColumnDeclaration("Опт. надб. %", 32),
				new PrintColumnDeclaration("Отпуск. цена пост-ка без НДС, руб", 56),
				new PrintColumnDeclaration("НДС пост-ка, руб", 40),
				new PrintColumnDeclaration("Отпуск. цена пост-ка с НДС, руб", 56),
				new PrintColumnDeclaration("Розн. торг. надб. %", 33),
				new PrintColumnDeclaration("Розн. цена. за ед., руб", 56),
				new PrintColumnDeclaration("Кол-во", 36),
				new PrintColumnDeclaration("Розн. сумма, руб", 56),
			};
			var columnGrops = new [] {
				new ColumnGroup("Предприятие - изготовитель", 5, 6)
			};
			var rows = waybill.Lines.Select((l, i) => new object[] {
				++i,
				l.Product,
				l.SerialNumber,
				l.Certificates,
				l.Period,
				string.Format("{0} {1}", l.Producer, l.Country),
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

			var block = Block("Продажная сумма: " + RusCurrency.Str((double)waybill.RetailSum));
			block.Inlines.Add(new Figure(new Paragraph(new Run(waybill.RetailSum.ToString()))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});

			block = Block("Сумма поставки: " + RusCurrency.Str((double)waybill.Sum));
			block.Inlines.Add(new Figure(new Paragraph(new Run(waybill.Sum.ToString()))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});

			TwoColumns();
			Block(String.Format("Затребовал:  {0}\n\n", docSettings.ReqestedBy)
				+ "место печати       подпись     _________________\n\n"
				+ "\" ____\" _______________20__г\n");
			Block(String.Format("Отпустил: Сдал (выдал)________________{0}\n\n", docSettings.SentBy)
				+ String.Format("Получил:Принял(получил)______________{0}\n\n", docSettings.GotBy)
				+ String.Format("Руководитель учреждения_____________{0}\n\n", settings.Director)
				+ String.Format("Главный (старший)бухгалтер ________________{0}\n", settings.Accountant));
			return doc;
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
			return new Paragraph(new Run(string.Format("страница {0} из {1}", page + 1, pageCount))) {
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

	public class ColumnGroup
	{
		public string Name;
		public int First;
		public int Last;

		public ColumnGroup(string name, int first, int last)
		{
			Name = name;
			First = first;
			Last = last;
		}
	}
}