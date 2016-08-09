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
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class ChecksView : ViewModelFixture<Checks>
	{
		[Test]
		public void Export_check()
		{
			var result = (OpenResult)model.ExportExcel();

			Assert.IsTrue(File.Exists(result.Filename));
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
			session.Save(check);
			Assert.AreEqual(0, model.Items.Value.Count);
		}

		[Test]
		public void Filter_by_address()
		{
			session.DeleteEach(session.Query<Address>().Skip(1));

			var newAddress = new Address("Тестовый адрес доставки");
			session.Save(newAddress);
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);
			MakeOrder(offer, newAddress);

			model.AddressSelector.All.Value = true;
			Assert.That(model.Items.Value.Count, Is.EqualTo(2));
			model.AddressSelector.Addresses[1].IsSelected = false;
			Assert.That(model.Items.Value.Count, Is.EqualTo(2));
			scheduler.AdvanceByMs(1000);
			Assert.That(model.Items.Value.Count, Is.EqualTo(1));
		}
	}
}
