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
			var reloaded = model.Waybills.Value.First(w => w.Id == waybill.Id);
			Assert.That(reloaded.RetailSum, Is.GreaterThan(retailSum));
		}

		[Test]
		public void Create()
		{
			var result = model.Create().GetEnumerator();
			Assert.IsTrue(result.MoveNext());
			var dialog = ((CreateWaybill)((DialogResult)result.Current).Model);
			dialog.Waybill.ProviderDocumentId = "1";
			dialog.Waybill.UserSupplierName = "test";
			result.MoveNext();
			Assert.IsNotNull(dialog.Waybill.Address);
			Assert.AreEqual(dialog.Waybill.Address.Id, address.Id);
			Assert.Contains(dialog.Waybill.Id, model.Waybills.Value.Select(w => w.Id).ToArray());
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
		public void Waybil_markup_report()
		{
			var seesion = FixtureHelper.GetFactory().OpenSession();
			session.CreateSQLQuery(@"
insert into BarCodes (value) values ('first_test_bar');
").ExecuteUpdate();
			var waybill = new Waybill()
			{
				DocumentDate = DateTime.Parse($"03.03.{DateTime.Now.Year - 1}"),
				Sum = 500,
				RetailSum = 450,
				TaxSum = 50,			
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

			seesion.Save(waybill);
			session.Save(waybillLineOne);
			session.Save(waybillLineTwo);

			try
			{
				FileHelper.InitDir(settings.MapPath("Reports"));

				var result = model.WaybillMarkupReport(true, true).GetEnumerator();
				var task = Next<TaskResult>(result);
				task.Task.Start();
				task.Task.Wait();
				var openWithNds = Next<OpenResult>(result);

				Assert.IsTrue(File.Exists(openWithNds.Filename), openWithNds.Filename);
				Assert.That(openWithNds.Filename, Does.Contain($"ЖНВЛС-{ DateTime.Today.Year - 1}"));
				Assert.That(File.ReadAllText(openWithNds.Filename).Length, Is.GreaterThan(1));

				var stream = new MemoryStream(File.ReadAllBytes(openWithNds.Filename));
				var workbook = new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
				stream.Close();
				var sheet = workbook[0];

				var enumenator = sheet.GetRowEnumerator();
				while (enumenator.MoveNext())
				{
					Assert.IsTrue((enumenator.Current as NPOI.HSSF.UserModel.HSSFRow).Cells.Count > 0);
					if ((enumenator.Current as NPOI.HSSF.UserModel.HSSFRow).Cells[0].ToString() == "first_test_bar")
					{
						break;
					}
				}

				
				var inExcel = (enumenator.Current as NPOI.HSSF.UserModel.HSSFRow).Cells.ToArray();
				var isRightCalculated = checkClauclate(inExcel, waybillLineOne, waybillLineTwo, 1.1);

				Assert.IsTrue(isRightCalculated);

				result = model.WaybillMarkupReport(true, false).GetEnumerator();
				task = Next<TaskResult>(result);
				task.Task.Start();
				task.Task.Wait();
				var openWithoutNds = Next<OpenResult>(result);


				Assert.IsTrue(File.Exists(openWithoutNds.Filename), openWithoutNds.Filename);
				Assert.That(openWithoutNds.Filename, Does.Contain($"ЖНВЛС-{ DateTime.Today.Year - 1}"));
				Assert.That(File.ReadAllText(openWithoutNds.Filename).Length, Is.GreaterThan(1));

				stream = new MemoryStream(File.ReadAllBytes(openWithNds.Filename));
				workbook = new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
				stream.Close();
				sheet = workbook[0];

				enumenator = sheet.GetRowEnumerator();
				while (enumenator.MoveNext())
				{
					Assert.IsTrue((enumenator.Current as NPOI.HSSF.UserModel.HSSFRow).Cells.Count > 0);
					if ((enumenator.Current as NPOI.HSSF.UserModel.HSSFRow).Cells[0].ToString() == "first_test_bar")
					{
						break;
					}
				}


				inExcel = (enumenator.Current as NPOI.HSSF.UserModel.HSSFRow).Cells.ToArray();
				isRightCalculated = checkClauclate(inExcel, waybillLineOne, waybillLineTwo, 1.0);

				Assert.IsTrue(isRightCalculated);

			}
			catch (Exception exc)
			{
				session.CreateSQLQuery($@"
delete from waybilllines where EAN13 = 'first_test_bar';
delete from BarCodes where value = 'first_test_bar';
delete from Waybills where id = {waybill.Id}
").ExecuteUpdate();
				session.Close();

				throw exc;
			}		
		}

		private bool checkClauclate(NPOI.SS.UserModel.ICell[] inExcel, WaybillLine waybillLineOne, WaybillLine waybillLineTwo, double k)
		{
			bool itsOk = true;
			var slasher = new Helpers.SlashNumber();			
			if (inExcel[0].ToString() != "first_test_bar")
			{
				itsOk = false;
			}
			if(Convert.ToDouble(inExcel[1].ToString()) != (waybillLineOne.Quantity + waybillLineTwo.Quantity) * 1.0 / 1000)
			{
				itsOk = false;
			}
			if(Convert.ToDecimal(inExcel[2].ToString()) != (waybillLineOne.Quantity * waybillLineOne.SupplierCost + waybillLineTwo.Quantity * waybillLineTwo.SupplierCost) / 1000 * (decimal)k)
			{
				itsOk = false;
			}
			if(Convert.ToDecimal(inExcel[3].ToString()) != (waybillLineOne.Quantity * waybillLineOne.RetailCost + waybillLineTwo.Quantity * waybillLineTwo.RetailCost) / 1000 * (decimal)k)
			{
				itsOk = false;
			}
			if(Convert.ToDecimal(inExcel[4].ToString()) !=
					 slasher.Convert(((decimal)(waybillLineOne.Quantity * waybillLineOne.ProducerCost) + Math.Round((decimal)(waybillLineTwo.Quantity * waybillLineTwo.ProducerCost), 2)) / 1000 * (decimal)k, 5))
			{
				itsOk = false;
			}
			if(Convert.ToDecimal(inExcel[5].ToString()) != Math.Min((decimal)waybillLineOne.RegistryCost, (decimal)waybillLineTwo.RegistryCost))
			{
				itsOk = false;
			}
			if(Convert.ToDecimal(inExcel[6].ToString()) != (decimal)((waybillLineOne.Quantity + waybillLineTwo.Quantity) * 1.0 / 1000))
			{
				itsOk = false;
			}
			if(Convert.ToDecimal(inExcel[7].ToString()) !=
					 (Math.Round((decimal)(waybillLineOne.RetailCost + waybillLineTwo.RetailCost) / 2, 2) -
					 Math.Round((decimal)(waybillLineOne.SupplierCost + waybillLineTwo.SupplierCost) / 2, 2))
					 / 1000
					 )
			{
				itsOk = false;
			}

			return itsOk;
		}
	}
}