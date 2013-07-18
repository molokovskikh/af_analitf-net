using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class WaybillDetailFixture : BaseFixture
	{
		private WaybillDetails model;

		[SetUp]
		public void Setup()
		{
			var waybill = data.CreateWaybill(address, settings);
			model = Init(new WaybillDetails(waybill.Id));
		}

		[Test]
		public void Tax_filter()
		{
			Assert.AreEqual("Все, Нет значения, 10", model.Taxes.Implode(t => t.Name));
			Assert.AreEqual("Все", model.CurrentTax.Value.Name);
			model.CurrentTax.Value = model.Taxes.First(t => t.Value == 10);
			Assert.AreEqual(1, model.Lines.Value.Count);
		}

		[Test]
		public void Recalculate_waybill()
		{
			Assert.AreEqual(24, model.Lines.Value[0].RetailCost);
			Assert.IsTrue(model.RoundToSingleDigit);
			model.RoundToSingleDigit.Value = false;
			Assert.AreEqual(24.09, model.Lines.Value[0].RetailCost);
		}

		[Test]
		public void Open_waybill()
		{
			Assert.IsNotNull(model.Waybill);
			Assert.IsNotNull(model.Lines);
		}

		[Test]
		public void Reload_settings_on_change()
		{
			restore = true;
			Assert.AreEqual(24, model.Lines.Value[0].RetailCost);
			var settings = Init<SettingsViewModel>();
			settings.Markups[0].Markup = 50;
			settings.Markups[0].MaxMarkup = 50;
			settings.Save();
			Close(settings);
			testScheduler.AdvanceByMs(1000);
			Assert.AreEqual("", manager.MessageBoxes.Implode());
			Assert.AreEqual(30.1, model.Lines.Value[0].RetailCost);
		}

		[Test, RequiresSTA]
		public void Print_racking_map()
		{
			var result = (DialogResult)model.PrintRackingMap();
			var preview = ((PrintPreviewViewModel)result.Model);
			Assert.IsNotNull(preview);
		}

		[Test, RequiresSTA]
		public void Print_invoice()
		{
			var result = (DialogResult)model.PrintInvoice().First();
			var preview = ((PrintPreviewViewModel)result.Model);
			Assert.IsNotNull(preview.Document);
		}
	}
}