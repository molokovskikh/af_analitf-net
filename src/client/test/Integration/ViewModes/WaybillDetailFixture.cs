using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI.Testing;
using Test.Support.log4net;
using CreateWaybill = AnalitF.Net.Client.Test.Fixtures.CreateWaybill;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class WaybillDetailFixture : ViewModelFixture
	{
		private WaybillDetails model;
		private Waybill waybill;

		[SetUp]
		public void Setup()
		{
			waybill = Fixture<LocalWaybill>().Waybill;
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
			var waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(24.8, waybillLine.RetailCost);
			Assert.IsTrue(model.RoundToSingleDigit);
			model.RoundToSingleDigit.Value = false;
			waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(24.82, waybillLine.RetailCost);
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
			var waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(24.8, waybillLine.RetailCost);
			var settings = Init<SettingsViewModel>();
			settings.Markups[0].Markup = 50;
			settings.Markups[0].MaxMarkup = 50;
			settings.Save();
			Close(settings);
			scheduler.AdvanceByMs(1000);
			Assert.AreEqual("", manager.MessageBoxes.Implode());
			waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(30.8, waybillLine.RetailCost);
		}

		[Test]
		public void Print_waybill()
		{
			var results = model.PrintWaybill();
			var settings = (SimpleSettings)((DialogResult)results.First()).Model;
			Assert.That(settings.Properties.Count(), Is.GreaterThan(0));
		}

		[Test]
		public void Print_racking_map()
		{
			var result = (DialogResult)model.PrintRackingMap();
			var preview = ((PrintPreviewViewModel)result.Model);
			Assert.IsNotNull(preview);
		}

		[Test]
		public void Print_invoice()
		{
			var result = (DialogResult)model.PrintInvoice().First();
			var preview = ((PrintPreviewViewModel)result.Model);
			Assert.IsNotNull(preview.Document);
		}

		[Test]
		public void Create_new_line()
		{
			waybill.IsCreatedByUser = true;
			var waybillLine = new WaybillLine();
			model.Lines.Value.AddNewItem(waybillLine);
			Assert.AreEqual(11, model.Waybill.Lines.Count);
			Assert.AreEqual(waybillLine.Waybill.Id, model.Waybill.Id);
		}

		[Test]
		public void Load_certificate()
		{
			Env.Scheduler = ImmediateScheduler.Instance;

			var waybillFixture = Fixture<CreateWaybill>();
			var fixture = new CreateCertificate {
				Waybill = waybillFixture.Waybill
			};
			Fixture(fixture);
			var line = fixture.Line;
			var waybillId = line.Waybill.Log.Id;

			var updateResults = shell.Update().ToArray();
			model = Init(new WaybillDetails(waybillId));
			Assert.AreEqual(2, updateResults.Length,
				"должны были получить результат открытия файла накладной и оповещение о новой накладной {0}", updateResults.Implode());
			Assert.IsInstanceOf<DialogResult>(updateResults[0]);
			Assert.IsInstanceOf<OpenResult>(updateResults[1]);

			var downloaded = model.Download(model.Lines.Value.Cast<WaybillLine>().First(l => l.Id == line.Id)).ToArray();
			Assert.AreEqual(0, downloaded.Length, downloaded.Implode());
			Assert.AreEqual(String.Format("Файл 'Сертификаты для {0} серия {1}' загружен", line.Product, line.SerialNumber), WaitNotification());
		}

		[Test]
		public void Persis_registry_config()
		{
			var results = model.PrintRegistry().GetEnumerator();
			Assert.IsTrue(results.MoveNext());
			var target = (RegistryDocumentSettings)((SimpleSettings)((DialogResult)results.Current).Model).Target;
			target.CommitteeMember1 = "Член комитета №1";
			Assert.IsTrue(results.MoveNext());
			Assert.IsNotNull(results.Current);
			Close(model);

			model = Init(new WaybillDetails(waybill.Id));
			results = model.PrintRegistry().GetEnumerator();
			Assert.IsTrue(results.MoveNext());
			target = (RegistryDocumentSettings)((SimpleSettings)((DialogResult)results.Current).Model).Target;
			Assert.AreEqual("Член комитета №1", target.CommitteeMember1);
		}
	}
}