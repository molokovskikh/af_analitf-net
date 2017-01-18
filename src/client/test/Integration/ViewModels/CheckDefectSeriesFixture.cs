using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Results;
using System.IO;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class CheckDefectSeriesFixture : ViewModelFixture<CheckDefectSeries>
	{

		[Test]
		public void Export_export()
		{
			var result = (OpenResult)model.ExportExcel();

			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_defect_stock()
		{
			var results = model.PrintDefectStock().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Mark()
		{
			var reject = session.Query<Reject>().First();
			var stock = new Stock()
			{
				Product = reject.Product,
				SerialNumber = reject.Series,
				Status = StockStatus.Available,
				Quantity = 10,
				SupplyQuantity = 10
			};
			session.Save(stock);
			Assert.IsTrue(stock.RejectStatus == RejectStatus.Unknown);

			model.Begin.Value = reject.LetterDate.AddDays(-1);
			model.End.Value = reject.LetterDate.AddDays(1);
			ForceInit();
			// статус Возможно рассчитывается динамически и не сохраняется в базе
			var tempStock = model.Items.Value.Single(x => x.Id == stock.Id);
			Assert.IsTrue(tempStock.RejectStatus == RejectStatus.Perhaps);
			Assert.IsTrue(stock.RejectStatus == RejectStatus.Unknown);

			model.CurrentItem.Value = tempStock;

			var seq = model.EnterItems().GetEnumerator();
			seq.MoveNext();
			var edit = ((EditDefectSeries)((DialogResult)seq.Current).Model);
			edit.Ok();
			seq.MoveNext();

			// статусы Брак  и Не брак сохраняются в базе
			session.Refresh(stock);
			Assert.IsTrue(stock.RejectStatus == RejectStatus.Defective);
		}
	}
}