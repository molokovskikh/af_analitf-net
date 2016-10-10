using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Print
{
	public class ReturnWaybill : BaseDocument
	{
		private ReturnToSupplier returnToSupplier;
		private WaybillSettings waybillSettings;
		public ReturnWaybill(ReturnToSupplier _returnToSupplier, WaybillSettings _waybillSettings)
		{
			returnToSupplier = _returnToSupplier;
			waybillSettings = _waybillSettings;
			doc.PagePadding = new Thickness(29);
			((IDocumentPaginatorSource)doc).DocumentPaginator.PageSize = new Size(1069, 756);

			BlockStyle = new Style(typeof(Paragraph)) {
				Setters = {
					new Setter(Control.FontSizeProperty, 10d),
					new Setter(System.Windows.Documents.Block.MarginProperty, new Thickness(0, 3, 0, 3))
				}
			};

			HeaderStyle = new Style(typeof(Run), HeaderStyle) {
				Setters = {
					new Setter(Control.FontSizeProperty, 12d),
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
			var headers = new[]
			{
				new PrintColumn("№ чека", 45),
				new PrintColumn("Дата", 90),
				new PrintColumn("ККМ", 90),
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

			/*var rows = _checks.Select((o, i) => new object[]
			{
				o.Id,
				o.Date.ToString("dd/M/yyyy"),
				o.KKM,
				o.Department.Name,
				o.Cancelled,
				o.RetailSum,
				o.DiscountSum,
				o.Sum,
			});

			BuildTable(rows, headers, columnGrops);*/
		}
	}
}
