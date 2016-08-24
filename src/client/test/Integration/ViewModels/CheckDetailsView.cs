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
	public class CheckDetailsView : ViewModelFixture
	{
		[Test]
		public void Export_check_details()
		{
			var check = CreateCheck();
			var model = Open(new CheckDetails(check.Id));
			var result = (OpenResult)model.ExportExcel();

			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_checksDetails()
		{
			var check = CreateCheck();
			var model = Open(new CheckDetails(check.Id));
			var results = model.PrintCheckDetails().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		private Check CreateCheck()
		{
			session.DeleteEach<Check>();
			var department = session.Query<Address>().First();
			var check = new Check
			{
				CheckType = CheckType.CheckReturn,
				Date = DateTime.Today.AddDays(-7),
				ChangeOpening = DateTime.Today.AddDays(-7),
				Status = Status.Open,
				Clerk = "Тестовый кассир",
				Department = department,
				KKM = "1(0000000)",
				PaymentType = PaymentType.Cash,
				SaleType = SaleType.FullCost,
				Discont = 10,
				ChangeId = 0,
				ChangeNumber = 42,
				Cancelled = false,
			};
			var checkLine = CreateCheckLine(check);
			session.Save(check);
			session.Save(checkLine);
			check.Lines.Add(checkLine);
			return check;
		}

		private CheckLine CreateCheckLine(Check check)
		{
			return new CheckLine(check.Id)
			{
				Barcode = "124",
				ProductId = 10,
				ProducerId = 12,
				Product = "Тестовый продукт",
				RetailCost = 110,
				Cost = 100,
				Quantity = 1,
				DiscontSum = 10,
				CheckId = 0,
				ProductKind = 1,
			};
		}
	}
}
