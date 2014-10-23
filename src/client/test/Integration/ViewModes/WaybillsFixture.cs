using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
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
			var file = Path.Combine(settings.MapPath("Waybills"), string.Format("{0}_{1}.txt",
				waybill.Id,
				waybill.Supplier.Name));
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
			settings.Markups[0].Markup = 50;
			settings.Markups[0].MaxMarkup = 50;
			settings.Save();
			Close(settings);
			testScheduler.AdvanceByMs(50);

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
	}
}