using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Inventory;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	class StocksFixture : ViewModelFixture<Stocks>
	{
		[Test]
		public void Export_export()
		{
			var result = (OpenResult) model.ExportExcel();

			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_racking_map()
		{
			var result = (DialogResult) model.PrintStockRackingMaps();
			var preview = ((PrintPreviewViewModel)result.Model);

			Assert.IsNotNull(preview);
		}

		[Test]
		public void Print_price_tags()
		{
			var result = (DialogResult)model.PrintStockPriceTags();
			var preview = ((PrintPreviewViewModel)result.Model);

			Assert.IsNotNull(preview);
		}

		[Test]
		public void Print_stock()
		{
			var results = model.PrintStock().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_stockLimitMonth()
		{
			var results = model.PrintStockLimitMonth().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}
	}
}
