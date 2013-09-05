using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using NHibernate.Linq;
using NPOI.HSSF.UserModel;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class ExportFixture : BaseViewFixture
	{
		private OpenResult result;

		[TearDown]
		public void TearDown()
		{
			if (result != null)
				File.Delete(result.Filename);
		}

		[Test]
		public void Export()
		{
			var catalog = session.Query<Catalog>().First(c => c.HaveOffers);
			var model = new CatalogOfferViewModel(catalog);
			CheckExport(model);
		}

		[Test]
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
			Init(model);
			InitView(model);

			Assert.That(model.CanExport, Is.True);
			result = (OpenResult)model.Export();
			Assert.That(File.Exists(result.Filename), result.Filename);
		}
	}
}