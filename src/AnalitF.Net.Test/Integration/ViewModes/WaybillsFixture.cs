using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class WaybillsFixture : BaseFixture
	{
		private WaybillsViewModel model;
		private Waybill waybill;

		[SetUp]
		public void Setup()
		{
			waybill = data.CreateWaybill(address, settings);
			model = Init<WaybillsViewModel>();
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
	}
}