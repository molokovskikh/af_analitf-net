using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	class ShelfLifeDocument : BaseDocument
	{
		private Stock[] _stocks;
		private Dictionary<string, bool> _visibilityDic;

		public ShelfLifeDocument(Stock[] stocks, Dictionary<string, bool> visibilityDic)
		{
			_stocks = stocks;
			_visibilityDic = visibilityDic;
		}

		protected override void BuildDoc()
		{
			var headObj = new[] {
				Tuple.Create("Period", new PrintColumn("Срок годности", 80)),
				Tuple.Create("Product", new PrintColumn("Торговое наименование", 170)),
				Tuple.Create("SerialNumber", new PrintColumn("Серия", 90)),
				Tuple.Create("Producer", new PrintColumn("Производитель", 120)),
				Tuple.Create("Quantity", new PrintColumn("Кол-во", 50)),
				Tuple.Create("WaybillNumber", new PrintColumn("Номер накладной", 80)),
				Tuple.Create("SupplierFullName", new PrintColumn("Поставщик", 120)),
			};
			var headers = headObj.Where(x => _visibilityDic.ContainsKey(x.Item1) ? _visibilityDic[x.Item1] : true).Select(x => x.Item2).ToArray();
			var table = BuildTableHeader(headers, null);

			Func<Stock, object[]> toRow = x => {
				var obj = new[] {
					Tuple.Create("Period", (object)x.Period),
					Tuple.Create("Product", (object)x.Product),
					Tuple.Create("SerialNumber", (object)x.SerialNumber),
					Tuple.Create("Producer", (object)x.Producer),
					Tuple.Create("Quantity", (object)x.Quantity),
					Tuple.Create("WaybillNumber", (object)x.WaybillNumber),
					Tuple.Create("SupplierFullName", (object)x.SupplierFullName),
				};
				return Remap(obj, _visibilityDic);
			};

			var tableRowGroup = table.RowGroups[0];
			var groups = _stocks.GroupBy(x => x.PeriodMonth);
			foreach (var group in groups) {
				var row = new TableRow();
				tableRowGroup.Rows.Add(row);
				var cell = Cell("Срок годности истекает: " + group.Key);
				if (_visibilityDic.Any())				
					cell.ColumnSpan = _visibilityDic.Count(x => x.Value);
				row.Cells.Add(cell);
				var rows = group.Select(toRow);
				BuildRows(rows, headers, table);
			}

			doc.Blocks.Add(table);
		}

		private static object[] Remap(Tuple<string, object>[] objects, Dictionary<string, bool> dic)
		{
			return objects.Where(x => dic.ContainsKey(x.Item1) ? dic[x.Item1] : true).Select(x => x.Item2).ToArray();
		}
	}
}
