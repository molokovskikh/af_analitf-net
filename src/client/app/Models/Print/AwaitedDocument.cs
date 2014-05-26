using System.Collections.Generic;
using System.Linq;

namespace AnalitF.Net.Client.Models.Print
{
	public class AwaitedDocument : BaseDocument
	{
		private IEnumerable<AwaitedItem> items;

		public AwaitedDocument(IEnumerable<AwaitedItem> items)
		{
			this.items = items;
		}

		protected override void BuildDoc()
		{
			Header("Ожидаемые позиции");
			var headers = new[] {
				new PrintColumn("Наименование", 350),
				new PrintColumn("Производитель", 350),
			};
			var rows = items.Select(r => new object[] {
				r.Catalog.FullName,
				r.ProducerName,
			});
			BuildTable(rows, headers);
		}
	}
}