using AnalitF.Net.Client.Helpers;
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

		[Test(Description = "Тест на валидность штрих-кодов")]
		public void IsValidBarCode()
		{
			string[] valid = new[] {
						"085126880552",
						"0085126880552",
						"00085126880552",
						"0786936226355",
						"0719852136552"
				};
			foreach (var s in valid)
				Assert.IsTrue(Util.IsValidBarCode(s));
		}

		[Test(Description = "Тест на невалидность штрих-кодов")]
		public void IsInValidBarCode()
		{
			string[] invalid = new[] {
						"0058126880552",
						"58126880552",
						"0786936223655",
						"0719853136552",
						"",
						"00",
						null,
						"123456789123456789123456789",
						"1111111111111"
				};
			foreach (var s in invalid)
				Assert.IsFalse(Util.IsValidBarCode(s));
		}
	}
}
