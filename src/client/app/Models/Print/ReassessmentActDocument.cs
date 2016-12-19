using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Views.Inventory.PrintForm;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace AnalitF.Net.Client.Models.Print
{
	class ReassessmentActDocument : BaseDocument
	{
		private ReassessmentLine[] _items;

		public ReassessmentActDocument(ReassessmentLine[] items)
		{
			_items = items;
			this.doc = new ReassessmentActDocumentForm();
			Configure();
		}

		protected override void BuildDoc()
		{
			var rows = _items.Select((o, i) => new object[]
			{
				$"{o.Product} {o.Producer}",
				o.Quantity,
				o.SupplierCost,
				o.SrcRetailCost,
				o.RetailCost,
				o.RetailCost - o.SrcRetailCost,
				o.SrcRetailSum,
				o.RetailSum,
				o.RetailSum - o.SrcRetailSum,
				o.SrcRetailMarkup,
				o.RetailMarkup,
			});

			var table = (Table)doc.Blocks.Single(x => x.Name == "Table");
			var tableRowGroup = table.RowGroups[1];
			foreach (var data in rows)
				BuildRow(tableRowGroup, null, data);
		}
	}
}
