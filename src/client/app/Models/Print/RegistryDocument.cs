﻿using System;
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
	[Description("Настройка печати реестра")]
	public class RegistryDocumentSettings
	{
		public RegistryDocumentSettings Setup(Waybill waybill)
		{
			RegistryId = waybill.ProviderDocumentId;
			Date = waybill.DocumentDate;
			return this;
		}

		[Display(Name = "Реестр №", Order = 0), Ignore]
		public string RegistryId { get; set; }

		[Display(Name = "Дата", Order = 1), Ignore]
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

		public RegistryDocument(Waybill waybill, IList<WaybillLine> lines)
		{
			this.waybill = waybill;
			this.lines = lines;
			doc.PagePadding = new Thickness(29);
			//мнения о размере страницы разошлись
			//придерживаемся мнения delphi тк размеры колонок скопированы от туда
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			settings = waybill.WaybillSettings;
			docSettings = waybill.GetRegistryDocSettings();
			Settings = docSettings;

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
			TableStyle = new Style(typeof(Table), TableStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
				}
			};
		}

		protected override void BuildDoc()
		{
			var block = new Paragraph { Style = BlockStyle };
			//Figure - игнорирует отступы
			block.Inlines.Add(new Figure(new Paragraph(new Run(
				"К положению о порядке формирования цен на лекарственные средства и изделия медицинского назначения\n"
				+ "Цены реестра соответствуют\n"
				+ "Гос. реестру 17 изд. 5 доп."))) {
					Width = new FigureLength(208),
					HorizontalAnchor = FigureHorizontalAnchor.PageLeft,
					Padding = new Thickness(29, 0, 0, 0),
					TextAlignment = TextAlignment.Left
				});
			//WrapDirection = WrapDirection.None нужно что бы блок с "УТВЕРЖДАЮ"
			//не пытался заполнить пустое пространство между этим блоком и предыдущим
			block.Inlines.Add(new Figure(new Paragraph(new Run(settings.FullName))) {
				HorizontalOffset = 713,
				HorizontalAnchor = FigureHorizontalAnchor.PageLeft,
				WrapDirection = WrapDirection.None,
				Padding = new Thickness(0, 0, 29, 0)
			});
			doc.Blocks.Add(block);

			block = new Paragraph { Style = BlockStyle };
			//WrapDirection = WrapDirection.None нужно что бы текст заголовка не пытался заполнить пустое пространство
			//а располагался отдельно
			block.Inlines.Add(new Figure(new Paragraph(new Run("УТВЕРЖДАЮ\n"
				+ $"Зав.аптекой ______________{settings.Director}"))) {
					HorizontalAnchor = FigureHorizontalAnchor.PageRight,
					WrapDirection = WrapDirection.None,
					Padding = new Thickness(0, 0, 29, 0)
				});
			doc.Blocks.Add(block);

			var header = Header(String.Format("РЕЕСТР  №{0} от {1:d}\n"
				+ "розничных цен на лекарственные средства и изделия медицинского назначения,\n"
				+ "полученные от {4}-по счету (накладной) №{2} от {3:d}",
					docSettings.RegistryId,
					docSettings.Date,
					waybill.ProviderDocumentId,
					waybill.DocumentDate,
					waybill.SupplierName));
			header.TextAlignment = TextAlignment.Center;

			var columns = new[] {
				new PrintColumn("№ пп", 27),
				new PrintColumn("Наименование и краткая характеристика товара", 170, colSpan: 2),
				new PrintColumn(null, 30),
				new PrintColumn("Серия товара", 80),
				new PrintColumn("Срок годности", 60),
				new PrintColumn("Наименование", 95),
				new PrintColumn("Цена без НДС, руб", 50),
				new PrintColumn("Цена с НДС, руб", 50),
				new PrintColumn("Цена ГР, руб", 50),
				new PrintColumn("Опт. надб. %", 33),
				new PrintColumn("Отпуск. цена пост - ка без НДС, руб", 50),
				new PrintColumn("НДС пост-ка, руб", 40),
				new PrintColumn("Отпуск. цена пост-ка с НДС, руб", 50),
				new PrintColumn("Розн. торг. надб. %", 33),
        new PrintColumn("Розн. торг. надб. руб",50),
				new PrintColumn("Розн. цена. за ед., руб", 50),
				new PrintColumn("Кол-во", 36),
				new PrintColumn("Розн. сумма, руб", 50),
			};
			var columnGrops = new[] {
				new ColumnGroup("Предприятие - изготовитель", 4, 6)
			};
			var rows = lines.Select((l, i) => new object[] {
				++i,
				l.Product,
				l.ActualVitallyImportant ? "ЖВ" : "",
				l.SerialNumber,
				l.Period,
				$"{l.Producer} {l.Country}",
				l.ProducerCost != null ? l.ProducerCost.Value.ToString("0.00") : "",
				l.ProducerCostWithTax != null ? l.ProducerCostWithTax.Value.ToString("0.00") : "",
				l.RegistryCost != null ? l.RegistryCost.Value.ToString("0.00") : "",
				l.SupplierPriceMarkup,
				l.SupplierCostWithoutNds != null ? l.SupplierCostWithoutNds.Value.ToString("0.00") : "",
				l.TaxPerUnit,
				l.SupplierCost != null ? l.SupplierCost.Value.ToString("0.00") : "",
				l.RetailMarkup,
                l.RetailMarkupInRubles != null ? l.RetailMarkupInRubles.Value.ToString("0.00") : "",
				l.RetailCost != null ? l.RetailCost.Value.ToString("0.00") : "",
				l.Quantity,
				l.RetailSum != null ? l.RetailSum.Value.ToString("0.00") : "",
			});
			BuildTable(rows, columns, columnGrops);

			var retailsSum = lines.Sum(l => l.RetailSum);
			block = Block("Продажная сумма: " + (retailsSum != null ? RusCurrency.Str((double)retailsSum) : ""));
			block.Inlines.Add(new Figure(new Paragraph(new Run(retailsSum != null ? retailsSum.Value.ToString("0.00") : ""))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});
			var sum = lines.Sum(l => l.Amount);
			block = Block("Сумма поставки: " + (sum != null ? RusCurrency.Str((double)sum) : ""));
			block.Inlines.Add(new Figure(new Paragraph(new Run(sum != null ? sum.Value.ToString("0.00") : ""))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});
			Block("Члены комиссии:\n"
				+ $"_{docSettings.CommitteeMember1}____/             /\n"
				+ $"_{docSettings.CommitteeMember2}____/             /\n"
				+ $"_{docSettings.CommitteeMember3}____/             /");
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
	}
}