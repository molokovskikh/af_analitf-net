using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class CheckDetailsDocument : BaseDocument
	{
		private CheckLine[] _checkLines;
		private Check _header;

		public CheckDetailsDocument(CheckLine[] checkLines, Check header)
		{
			_checkLines = checkLines;
			_header = header;
		}

		protected override void BuildDoc()
		{
			var text = $"Чек {_header.Number}";
				doc.Blocks.Add(new Paragraph(new Run(text)) {
					FontWeight = FontWeights.Bold,
					FontSize = 16,
					TextAlignment = TextAlignment.Center
				});
			var headers = new[]
			{
				new PrintColumn("№", 60),
				new PrintColumn("Штрих-код", 90),
				new PrintColumn("Название товара", 90),
				new PrintColumn("Количество", 120),
				new PrintColumn("Цена розничная", 80),
				new PrintColumn("розничная", 60),
				new PrintColumn("скидки", 60),
				new PrintColumn("с учетом скидки", 60),
			};

			var columnGrops = new[]
			{
				new ColumnGroup("Сумма", 5, 7),
			};

			var count = _checkLines.Length;
			var TotalRetailCost = _checkLines.Sum(l => l.RetailCost);
			var TotalRetailSum = _checkLines.Sum(l => l.RetailSum);
			var TotalDiscontSum = _checkLines.Sum(l => l.DiscontSum);
			var TotalSum = _checkLines.Sum(l => l.Sum);
			var rows = _checkLines.Select((o, i) => new object[]
			{
				o.Id,
				o.Barcode,
				o.ProductName,
				o.Quantity,
				o.RetailCost,
				o.RetailSum,
				o.DiscontSum,
				o.Sum,
			});

			var table = BuildTable(rows, headers, columnGrops);

			if (count > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
						},
						new TableCell(new Paragraph(new Run()) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
						},
						new TableCell(new Paragraph(new Run(count.ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
						},
						new TableCell(new Paragraph(new Run(TotalRetailCost.ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(TotalRetailSum.ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(TotalDiscontSum.ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(TotalSum.ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Right
						}
					}
				});
			}
		}
	}
}
