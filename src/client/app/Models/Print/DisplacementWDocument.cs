using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Common.Tools;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	public class DisplacementWDocument : BaseDocument
	{
		private DisplacementDoc d;
		private DocumentTemplate template;
		private IList<DisplacementLine> lines;

		public DisplacementWDocument(DisplacementDoc d, IList<DisplacementLine> lines)
		{
			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			this.d = d;
			this.lines = lines;

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
			Header($"\n                               Наименование организации: \n"
				+ "             Отдел:______________________________________________\n");

			TwoColumns();
			var left = Header("Требование №________________________\n"
				+ "от \"_____\" _______________20__г\n"
				+ "кому: ___________________________________\n"
				+ "Основание отпуска________________________\n");
			left.TextAlignment = TextAlignment.Center;
			Header($"Накладная № {d.Id}\n"
				+ $"от  {d.Date:d}\n"
				+ $"Через кого   \n"
				+ "Доверенность № _______от \"______\" ________20__г\n");

			var columns = new[] {
				new PrintColumn("№ пп", 27),
				new PrintColumn("Наименование", 170),
				new PrintColumn("Производитель", 170),
				new PrintColumn("Серия", 80),
				new PrintColumn("Срок", 80),
				new PrintColumn("Цена", 80),
				new PrintColumn("Затребован.колич.", 80),
				new PrintColumn("Отпущен.колич.", 80),
				new PrintColumn("Сумма, руб", 80),
			};
			var rows = lines.Select((l, i) => new object[] {
				++i,
				l.Product,
				l.Producer,
				l.SerialNumber,
				l.Period,
				l.RetailCost,
				l.Quantity,
				l.Quantity,
				l.RetailSum
			});
			BuildTable(rows, columns);

			var retailSum = lines.Sum(l => l.RetailSum);
			var block = Block("Продажная сумма: " + RusCurrency.Str((double)retailSum));
			block.Inlines.Add(new Figure(new Paragraph(new Run(retailSum.ToString()))) {
				FontWeight = FontWeights.Bold,
				HorizontalAnchor = FigureHorizontalAnchor.ContentRight,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			});

			TwoColumns();
			Block($"Затребовал:  \n\n"
				+ "место печати       подпись     _________________\n\n"
				+ "\" ____\" _______________20__г\n");
			Block($"Отпустил: Сдал (выдал)________________\n\n"
				+ $"Получил:Принял(получил)______________\n\n"
				+ $"Руководитель учреждения_____________\n\n"
				+ $"Главный (старший)бухгалтер ________________\n");
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