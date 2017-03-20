using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	public class CheckDocument : BaseDocument
	{
		private Check[] _checks;

		public CheckDocument(Check[] checks)
		{
			_checks = checks;
		}

		protected override void BuildDoc()
		{
			var headers = new[]
			{
				new PrintColumn("№ чека", 45),
				new PrintColumn("Дата", 90),
				new PrintColumn("Кассир", 90),
				new PrintColumn("Отдел", 120),
				new PrintColumn("Аннулирован", 80),
				new PrintColumn("розничная", 80),
				new PrintColumn("скидки", 80),
				new PrintColumn("с учетом скидки", 80),
			};

			var columnGrops = new[]
			{
				new ColumnGroup("Сумма", 5, 7),
			};

			var rows = _checks.Select((o, i) => new object[]
			{
				o.Id,
				o.Date.ToShortDateString(),
				o.Clerk,
				o.Address.Name,
				o.Cancelled,
				o.RetailSum.ToString("0.00"),
				o.DiscountSum.ToString("0.00"),
				o.Sum.ToString("0.00"),
			});

			var table = BuildTable(rows, headers, columnGrops);
			table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 7,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(_checks.Sum(x => x.Sum).ToString("0.00"))) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Center
						},
					}
				});
			table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Наличными: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 7,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(_checks.Sum(x => x.Payment - x.Charge).ToString("0.00"))) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Center
						},
					}
				});
			table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Картой: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 7,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(_checks.Sum(x => x.PaymentByCard).ToString("0.00"))) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Center
						},
					}
				});

		}
	}
}
