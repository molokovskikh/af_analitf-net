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
using System.Collections.Generic;
using System;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class ChecksView : ViewModelFixture<Checks>
	{
		[Test]
		public void Export_export()
		{
			var result = (OpenResult)model.ExportExcel();

			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_checks()
		{
			var results = model.PrintChecks().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_return_act()
		{
			var results = model.PrintReturnAct().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		private Check CreateCheck()
		{
			return new Check
			{
				CheckType = CheckType.CheckReturn,
				Number = 100,
				Date = DateTime.Today.AddDays(-7),
				ChangeOpening = DateTime.Today.AddDays(-7),
				Status = Status.Open,
				Clerk = "Тестовый кассир",
				Department = address,
				KKM = "1(0000000)",
				PaymentType = PaymentType.Cash,
				SaleType = SaleType.FullCost,
				Discont = 10,
				ChangeId = 0,
				ChangeNumber = 42,
				Cancelled = false,
			};
		}

		private CheckLine CreateCheckLine(Check check)
		{
			return new CheckLine(check.Id)
			{
				Barcode = 124,
				ProductId = 10,
				ProducerId = 12,
				ProductName = "Тестовый продукт",
				RetailCost = 110,
				Cost = 100,
				Quantity = 1,
				DiscontSum = 10,
				CheckId = 0,
				ProductKind = 1,
			};
		}

		[Test]
		public void LoadData()
		{
			var address = session.Query<Address>().First();
			session.DeleteEach<Check>();
			var check = CreateCheck();
			var checkLine = CreateCheckLine(check);
			session.Save(check);
			session.Save(checkLine);
			check.Lines.Add(checkLine);
			Assert.AreEqual(1, model.Items.Value.Count);
			check.Date = DateTime.Today.AddDays(7);
			session.Update(check);
			Assert.AreEqual(0, model.Items.Value.Count);


			/*var reject = session.Query<Reject>().First();
			var stock = new Stock()
			{
				Product = reject.Product,
				Seria = reject.Series
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
			Assert.IsTrue(stock.RejectStatus == RejectStatus.Defective);*/
		}
	}
}
