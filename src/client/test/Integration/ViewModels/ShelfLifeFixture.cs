using System;
using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Results;
using System.IO;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class ShelfLifeFixture : ViewModelFixture<ShelfLife>
	{

		[Test]
		public void Export_export()
		{
			var result = (OpenResult)model.ExportExcel();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print()
		{
			var results = model.Print().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Filter()
		{
			var stock = new Stock()
			{
				Status = StockStatus.Available,
				Quantity = 10,
				Period = DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
			};
			session.Save(stock);
			// срок годности истёк
			Assert.IsTrue(stock.IsOverdue);

			var stock2 = new Stock()
			{
				Status = StockStatus.Available,
				Quantity = 10,
				Period = DateTime.Now.AddMonths(1).ToString("dd.MM.yyyy"),
			};
			session.Save(stock2);
			// срок годности не истёк
			Assert.IsFalse(stock2.IsOverdue);

			model.IsOverdue.Value = true;
			model.IsNotOverdue.Value = false;
			ForceInit();

			var isOverdueStock = model.Items.Value.SingleOrDefault(x => x.Id == stock.Id);
			Assert.IsNotNull(isOverdueStock);

			var isNotOverdueStock = model.Items.Value.SingleOrDefault(x => x.Id == stock2.Id);
			Assert.IsNull(isNotOverdueStock);
		}
	}
}