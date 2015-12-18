using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace AnalitF.Net.Client.Models.Print
{
	public class WaybillsDoc : BaseDocument
	{
		private Waybill[] waybills;

		public WaybillsDoc(Waybill[] waybills)
		{
			this.waybills = waybills;
		}

		protected override void BuildDoc()
		{
			Landscape();
			var headers = new[] {
				new PrintColumn("№", 90),
				new PrintColumn("№ поставщика", 90),
				new PrintColumn("Дата документа", 90),
				new PrintColumn("Дата получения документа", 200),
				new PrintColumn("Тип документа", 90),
				new PrintColumn("Поставщик", 200),
				new PrintColumn("Сумма опт", 90),
				new PrintColumn("Сумма розница", 90)
			};
			var rows = waybills.Select((o, i) => new object[] {
				o.Id,
				o.ProviderDocumentId,
				o.DocumentDate.ToShortDateString(),
				o.WriteTime,
				o.Type,
				o.SafeSupplier?.Name,
				o.Sum,
				o.RetailSum
			});

			var table = BuildTable(rows, headers);
			if (waybills.Length > 0) {
				table.RowGroups[0].Rows.Add(new TableRow {
					Cells = {
						new TableCell(new Paragraph(new Run("Итого: ")) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							ColumnSpan = 6,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(waybills.Sum(x => x.Sum).ToString())) {
							KeepTogether = true
						}) {
							Style = CellStyle,
							FontWeight = FontWeights.Bold,
							TextAlignment = TextAlignment.Right
						},
						new TableCell(new Paragraph(new Run(waybills.Sum(x => x.RetailSum).ToString())) {
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