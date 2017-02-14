using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class ExportFixture : BaseViewFixture
	{
		private OpenResult result;

		[TearDown]
		public void TearDown()
		{
			if (result != null)
				File.Delete(result.Filename);
		}

		[Test Ignore("тест конфликтует с WinForm.DataGridView")]
		public void Export()
		{
			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var model = new CatalogOfferViewModel(catalog);
			WpfTestHelper.WithWindow2(async w => {
				var view = Bind(model);
				w.Content = view;
				await view.WaitLoaded();

				Assert.IsTrue(model.CanExport.Value);
				result = (OpenResult)model.Export();
				Assert.That(File.Exists(result.Filename), result.Filename);
			});
		}

		[Test Ignore("тест конфликтует с WinForm.DataGridView")]
		public void Export_prices()
		{
			var model = new PriceViewModel();
			CheckExport(model);
			using(var file = File.OpenRead(result.Filename)) {
				var book = new HSSFWorkbook(file);
				var sheet = book.GetSheetAt(0);
				//флаг в Работе
				var cell = sheet.GetRow(1).GetCell(3);
				Assert.AreEqual("Да", cell.StringCellValue);
			}
		}

		private void CheckExport(BaseScreen model)
		{
			Bind(model);

			Assert.IsTrue(model.CanExport.Value);
			result = (OpenResult)model.Export();
			Assert.That(File.Exists(result.Filename), result.Filename);
		}
	}
}