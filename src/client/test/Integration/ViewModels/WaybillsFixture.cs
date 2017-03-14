using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI.Testing;
using CreateWaybill = AnalitF.Net.Client.ViewModels.Dialogs.CreateWaybill;
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels.Dialogs;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class WaybillsFixture : ViewModelFixture<WaybillsViewModel>
	{
		private Waybill waybill;

		[SetUp]
		public void Setup()
		{
			waybill = Fixture<LocalWaybill>().Waybill;
		}

		[Test]
		public void Load_waybills()
		{
			Assert.IsNotNull(model.Waybills.Value);
		}

		[Test]
		public void Alt_export()
		{
			var result = (OpenResult)model.AltExport();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Delete()
		{
			var waybill = model.Waybills.Value.First(w => w.Supplier != null);
			var file = Path.Combine(settings.MapPath("Waybills"), $"{waybill.Id}_{waybill.Supplier.Name}.txt");
			File.WriteAllText(file, "test content");
			model.CurrentWaybill.Value = waybill;
			model.SelectedWaybills.Add(waybill);
			model.Delete();
			Assert.That(model.Waybills.Value.Select(w => w.Id), Is.Not.Contains(waybill.Id));
			Assert.IsFalse(File.Exists(file));
		}

		[Test]
		public void Refresh_data_on_reactivate()
		{
			restore = true;

			model.CurrentWaybill.Value = model.Waybills.Value.First(w => w.Id == waybill.Id);
			model.EnterWaybill();
			Deactivate(model);

			var details = (WaybillDetails)shell.ActiveItem;
			var retailSum = details.Waybill.RetailSum;

			var settings = Init<SettingsViewModel>();
			settings.Markups.Value[0].Markup = 50;
			settings.Markups.Value[0].MaxMarkup = 50;
			var result = settings.Save().ToList();
			Close(settings);
			scheduler.AdvanceByMs(50);

			Assert.That(details.Waybill.RetailSum, Is.GreaterThan(retailSum));
			Close(details);
			Activate(model);
			scheduler.Start();
			var reloaded = model.Waybills.Value.First(w => w.Id == waybill.Id);
			Assert.That(reloaded.RetailSum, Is.GreaterThan(retailSum));
		}

		[Test]
		public void Create()
		{
			model.User.IsStockEnabled = false;
			var result = model.Create().GetEnumerator();
			Assert.IsTrue(result.MoveNext());
			var dialog = (CreateWaybill)((DialogResult)result.Current).Model;
			dialog.Waybill.ProviderDocumentId = "1";
			dialog.Waybill.UserSupplierName = "test";
			result.MoveNext();
			scheduler.Start();
			Assert.IsNotNull(dialog.Waybill.Address);
			Assert.AreEqual(dialog.Waybill.Address.Id, address.Id);
			Assert.Contains(dialog.Waybill.Id, model.Waybills.Value.Select(w => w.Id).ToArray());
		}

		[Test]
		public void Create_with_stock_enabled()
		{
			model.User.IsStockEnabled = true;
			var result = model.Create().GetEnumerator();
			Assert.IsTrue(result.MoveNext());
			var dialog = (CreateWaybill)((DialogResult)result.Current).Model;
			dialog.Waybill.ProviderDocumentId = "1";
			result.MoveNext();
			scheduler.Start();
			Assert.AreEqual("Собственный поставщик", dialog.Waybill.UserSupplierName);
			Assert.IsNotNull(dialog.Waybill.Address);
			Assert.AreEqual(dialog.Waybill.Address.Id, address.Id);
			Assert.Contains(dialog.Waybill.Id, model.Waybills.Value.Select(w => w.Id).ToArray());
		}

		[Test]
		public void Display_supplier_name()
		{
			var result = model.Create().GetEnumerator();
			Assert.IsTrue(result.MoveNext());
			var dialog = (CreateWaybill)((DialogResult)result.Current).Model;
			var supplier = CreateSupplier();
			dialog.Waybill = new Waybill(new Address("Тестовый адрес"), supplier);
			Assert.AreEqual(dialog.Waybill.SupplierName,supplier.FullName);
			result.MoveNext();
			dialog.Waybill.Supplier = null;
			Assert.AreEqual(dialog.Waybill.SupplierName,supplier.FullName);
		}

		public Supplier CreateSupplier()
		{
			var supplier = new Supplier();
			supplier.Name = "Тестовый поставщик";
			supplier.FullName = "Тестовый поставщик 1";
			return supplier;
		}

		[Test]
		public void Waybill_report()
		{
			FileHelper.InitDir(settings.MapPath("Reports"));
			var result = model.RegulatorReport().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();
			var open = Next<OpenResult>(result);
			Assert.IsTrue(File.Exists(open.Filename), open.Filename);
			Assert.That(open.Filename, Does.Contain("Росздравнадзор"));
		}

		[Test]
		public void Vitally_important_report()
		{
			var fixture = Fixture<LocalWaybill>();
			FileHelper.InitDir(settings.MapPath("Reports"));
			model.CurrentWaybill.Value = model.Waybills.Value.First(x => x.Id == fixture.Waybill.Id);
			var result = model.VitallyImportantReport().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();
			var open = Next<OpenResult>(result);
			Assert.IsTrue(File.Exists(open.Filename), open.Filename);
			Assert.That(open.Filename, Does.Contain("Росздравнадзор"));
			//должна быть строка заголовка и как минимум одна строка данных
			var text = File.ReadAllText(open.Filename);
			Assert.That(text.Length, Is.GreaterThan(1));
			//в 11 колонке будет vendorid
			var vendorId = text.Split(new [] { Environment.NewLine }, StringSplitOptions.None)[1].Split(';')[10];
			//в тестовых данных VendorId == Id
			Assert.AreEqual(vendorId, fixture.Waybill.Supplier.Id.ToString());
		}

		[Test]
		public void Waybill_mark_report_with_NDS()
		{
			WaybillMarkupReport(System.Windows.MessageBoxResult.Yes, 1.1);
		}

		[Test]
		public void Waybill_mark_report_without_NDS()
		{
			WaybillMarkupReport(System.Windows.MessageBoxResult.No, 1);
		}

		protected void WaybillMarkupReport(System.Windows.MessageBoxResult exceptResult, double k)
		{
			session.CreateSQLQuery(@"
insert into BarCodes (value) values ('first_test_bar');
").ExecuteUpdate();
			var waybill = new Waybill()
			{
				DocumentDate = DateTime.Parse($"03.03.{DateTime.Now.Year - 1}"),
			};

			var waybillLineOne = new WaybillLine(waybill)
			{
				EAN13 = "first_test_bar",
				Quantity = 2,
				SupplierCost = 300,
				RetailCost = 305,
				ProducerCost = (decimal)290.155,
				RegistryCost = 290,
			};

			var waybillLineTwo = new WaybillLine(waybill)
			{
				EAN13 = "first_test_bar",
				Quantity = 3,
				SupplierCost = 310,
				RetailCost = 315,
				ProducerCost = 300,
				RegistryCost = 300,
			};

			disposable.Add(Disposable.Create(() =>
			{
				var disposableSession = session;
				if (!session.IsOpen)
				{
					disposableSession = session.SessionFactory.OpenSession();
				}
				disposableSession.CreateSQLQuery($@"
				delete from waybilllines where EAN13 = 'first_test_bar';
				delete from BarCodes where value = 'first_test_bar';
				").ExecuteUpdate();
				disposableSession.Delete(waybill);
				if (!disposableSession.Equals(session))
				{
					disposableSession.Close();
				}
			}));

			session.Save(waybill);
			session.Save(waybillLineOne);
			session.Save(waybillLineTwo);

			FileHelper.InitDir(settings.MapPath("Reports"));

			manager.DefaultQuestsionResult = exceptResult;

			var result = model.WaybillMarkupReport().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();
			var open = Next<OpenResult>(result);

			Assert.IsTrue(File.Exists(open.Filename), open.Filename);
			Assert.That(open.Filename, Does.Contain($"ЖНВЛС-{ DateTime.Today.Year - 1}"));

			var stream = new MemoryStream(File.ReadAllBytes(open.Filename));
			var workbook = new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
			stream.Close();
			var sheet = workbook[0];

			var enumenator = sheet.GetRowEnumerator();
			while (enumenator.MoveNext())
			{
				var currentRow = (NPOI.HSSF.UserModel.HSSFRow)enumenator.Current;
				Assert.IsTrue(currentRow.Cells.Count > 0);
				if (currentRow.Cells[0].ToString() == "first_test_bar")
				{
					break;
				}
			}

			Assert.IsNotNull(enumenator.Current);

			var inExcel = (enumenator.Current as NPOI.HSSF.UserModel.HSSFRow).Cells.ToArray();

			Assert.AreEqual("first_test_bar", inExcel[0].ToString());

			Assert.AreEqual((decimal)0.005,
				Convert.ToDecimal(inExcel[1].ToString()), "(waybillLineOne.Quantity + waybillLineTwo.Quantity) / 1000 = 0.005");

			if(k == 1.1)
			{
				Assert.AreEqual((decimal)1.683,
				Convert.ToDecimal(inExcel[2].ToString()), $"(waybillLineOne.Quantity * waybillLineOne.SupplierCost + waybillLineTwo.Quantity * waybillLineTwo.SupplierCost) / 1000 * {k} = 1.683");

				Assert.AreEqual((decimal)1.7105,
				Convert.ToDecimal(inExcel[3].ToString()), $"(waybillLineOne.Quantity * waybillLineOne.RetailCost + waybillLineTwo.Quantity * waybillLineTwo.RetailCost) / 1000 * {k} = 1.7105");

				Assert.AreEqual((decimal)1.62834,
				Convert.ToDecimal(inExcel[4].ToString()), $@"Helpers.SlashNumber().Convert((waybillLineOne.Quantity * waybillLineOne.ProducerCost)
				+ Math.Round((decimal)(waybillLineTwo.Quantity * waybillLineTwo.ProducerCost), 2)) / 1000 * {k}, 5) = 1.62834");
			}

			if (k == 1.0)
			{
				Assert.AreEqual((decimal)1.53,
				Convert.ToDecimal(inExcel[2].ToString()), $"(waybillLineOne.Quantity * waybillLineOne.SupplierCost + waybillLineTwo.Quantity * waybillLineTwo.SupplierCost) / 1000 * {k} = 1.53");

				Assert.AreEqual((decimal)1.555,
				Convert.ToDecimal(inExcel[3].ToString()), $"(waybillLineOne.Quantity * waybillLineOne.RetailCost + waybillLineTwo.Quantity * waybillLineTwo.RetailCost) / 1000 * {k} = 1.555");

				Assert.AreEqual((decimal)1.48031,
				Convert.ToDecimal(inExcel[4].ToString()), $@"Helpers.SlashNumber().Convert((waybillLineOne.Quantity * waybillLineOne.ProducerCost)
				+ Math.Round((decimal)(waybillLineTwo.Quantity * waybillLineTwo.ProducerCost), 2)) / 1000 * {k}, 5) = 1.48031");
			}






			Assert.AreEqual((decimal)290.0, Convert.ToDecimal(inExcel[5].ToString()), "Math.Min((decimal)waybillLineOne.RegistryCost, (decimal)waybillLineTwo.RegistryCost) = 290.0");

			Assert.AreEqual((decimal)0.005, Convert.ToDecimal(inExcel[6].ToString()), "(waybillLineOne.Quantity + waybillLineTwo.Quantity) / 1000 = 0.005");

			Assert.AreEqual((decimal)0.005,
						Convert.ToDecimal(inExcel[7].ToString()), @"(Math.Round((decimal)(waybillLineOne.RetailCost + waybillLineTwo.RetailCost) / 2, 2) -
						Math.Round((decimal)(waybillLineOne.SupplierCost + waybillLineTwo.SupplierCost) / 2, 2)) / 1000 = 0.005");
		}
	}
}