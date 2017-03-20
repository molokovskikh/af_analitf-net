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
			CreateCheck();
			var result = (OpenResult)model.ExportExcel();

			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_return_act()
		{
			CreateCheck();
			var results = model.PrintReturnAct().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_check()
		{
			CreateCheck();
			var results = model.PrintChecks().GetEnumerator();
			var preview = Next<DialogResult>(results);

			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		private Check CreateCheck()
		{
			session.DeleteEach<Check>();
			var check = new Check(user, address, Enumerable.Empty<CheckLine>(), CheckType.CheckReturn)
			{
				Date = DateTime.Today.AddDays(-7),
				ChangeOpening = DateTime.Today.AddDays(-7),
				Clerk = "Тестовый кассир",
				KKM = "1(0000000)",
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
				SupplierCost = 100,
				Quantity = 1,
				DiscontSum = 10,
				CheckId = 0,
				ProductKind = 1,
			};
		}

		[Test]
		public void LoadData()
		{
			var check = CreateCheck();
			Assert.AreEqual(1, model.Items.Value.Count);
			session.DeleteEach<Check>();
			check.Date = DateTime.Today.AddDays(14);
			session.Save(check);
			model.Update();
			scheduler.AdvanceByMs(500);
			scheduler.Start();
			Assert.AreEqual(0, model.Items.Value.Count);
		}

		[Test]
		public void Filter_by_address()
		{
			restore = true;
			session.DeleteEach(session.Query<Address>().Skip(1));
			var newAddress = new Address("Тестовый адрес доставки");
			session.Save(newAddress);
			var check = CreateCheck();
			check.Address = newAddress;
			model.AddressSelector.All.Value = true;
			Assert.That(model.Items.Value.Count, Is.EqualTo(1));
			model.AddressSelector.All.Value = false;
			shell.CurrentAddress.Value = newAddress;
			Assert.That(model.Items.Value.Count, Is.EqualTo(1));
			model.AddressSelector.All.Value = false;
			shell.CurrentAddress.Value = null;
			scheduler.AdvanceByMs(500);
			scheduler.Start();
			Assert.That(model.Items.Value.Count, Is.EqualTo(0));
		}
	}
}
