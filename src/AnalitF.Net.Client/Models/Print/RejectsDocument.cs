using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models.Print
{
	public class RejectsDocument : BaseDocument
	{
		private IList<Reject> rejects;
		private bool showReason;

		public RejectsDocument(IList<Reject> rejects, bool showReason)
		{
			this.rejects = rejects;
			this.showReason = showReason;
		}

		public override FlowDocument Build()
		{
			var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
			var size = paginator.PageSize;
			paginator.PageSize = new Size(size.Height, size.Width);

			Header("Препараты, предписанные к изъятию из аптечной сети");
			var headers = new [] {
				new PrintColumnDeclaration("Серия", 96),
				new PrintColumnDeclaration("Наименование", 364),
				new PrintColumnDeclaration("Фирма-изготовитель", 208),
				new PrintColumnDeclaration("Номер письма", 180),
				new PrintColumnDeclaration("Дата", 148),
			};
			var rows = rejects.Select(r => new object[] {
				r.Series,
				r.Product,
				r.Producer,
				r.LetterNo,
				r.LetterDate.ToShortDateString()
			});
			if (showReason) {
				BuildTableWithReason(rows, headers);
			}
			else {
				BuildTable(rows, headers);
			}
			return doc;
		}

		private void BuildTableWithReason(IEnumerable<object[]> rows, PrintColumnDeclaration[] headers)
		{
			var table = BuildTableHeader(headers);
			var rowGroup = table.RowGroups[0];
			var i = 0;
			foreach (var row in rows) {
				BuildRow(headers, rowGroup, row);

				var reject = rejects[i];
				var tableRow = new TableRow();
				rowGroup.Rows.Add(tableRow);
				var cell = new TableCell(new Paragraph(new Run(reject.CauseRejects))) {
					ColumnSpan = headers.Length,
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(0, 0, 1, 1),
					FontStyle = FontStyles.Italic
				};
				tableRow.Cells.Add(cell);

				i++;
			}

			doc.Blocks.Add(table);
		}
	}
}