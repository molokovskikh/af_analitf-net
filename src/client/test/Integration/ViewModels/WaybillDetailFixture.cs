using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI.Testing;
using CreateWaybill = AnalitF.Net.Client.Test.Fixtures.CreateWaybill;
using System.IO;
using System.Windows.Documents;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class WaybillDetailFixture : ViewModelFixture
	{
		private WaybillDetails model;
		private Waybill waybill;

		[SetUp]
		public void Setup()
		{
			autoStartScheduler = false;
			waybill = Fixture<LocalWaybill>().Waybill;
			model = Open(new WaybillDetails(waybill.Id));
		}

		[Test]
		public void Tax_filter()
		{
			Assert.AreEqual("Все, Нет значения, 10", model.Taxes.Implode(t => t.Name));
			Assert.AreEqual("Все", model.CurrentTax.Value.Name);
			model.CurrentTax.Value = model.Taxes.First(t => t.Value == 10);
			Assert.AreEqual(4, model.Lines.Value.Count);
		}

		[Test]
		public void RetailMarkupInRubles_waybillLines()
		{
			var waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(4m, waybillLine.RetailMarkupInRubles);
		}

		[Test]
		public void RegistryDocument_waybillLines()
		{
			IList<WaybillLine> waybillLines = waybill.Lines;

			/* проверка надбавки в рублях в первой препарате */
			Assert.AreEqual(4m, waybillLines[0].RetailMarkupInRubles);

			var doc = new RegistryDocument(waybill, waybillLines);
			var flowDoc = doc.Build();

			List<TableCellCollection> listTableCellCollection = ((Table)flowDoc.Blocks.ToList().Where(x => x.GetType() == new Table().GetType()).First())
				.RowGroups.Select(x => x.Rows).ToList()
				.First().Select(x => x.Cells).ToList();

			TableCellCollection tableCellCollection = listTableCellCollection[0];

			/* проверка количества строк в таблице */
			Assert.AreEqual(15, listTableCellCollection.Count());

			/* проверяем названия столбцов */

			var ValueCollumnName = new TextRange(tableCellCollection[0].ContentStart, tableCellCollection[0].ContentEnd).Text;
			Assert.AreEqual("№ пп", ValueCollumnName);

			ValueCollumnName = new TextRange(tableCellCollection[3].ContentStart, tableCellCollection[3].ContentEnd).Text;
			Assert.AreEqual("Предприятие - изготовитель", ValueCollumnName);

			ValueCollumnName = new TextRange(tableCellCollection[11].ContentStart, tableCellCollection[11].ContentEnd).Text;
			Assert.AreEqual("Розн. торг. надб. руб", ValueCollumnName);

			/* Розн. торг. надб. руб проверяем значение в первом препарате */

			tableCellCollection = listTableCellCollection[2];

			var ValueCell = new TextRange(tableCellCollection[14].ContentStart, tableCellCollection[14].ContentEnd).Text;
			decimal? RetailMarkupInRubles = Convert.ToDecimal(ValueCell);
			Assert.AreEqual(4m, RetailMarkupInRubles);


			tableCellCollection = listTableCellCollection[3];
			ValueCell = new TextRange(tableCellCollection[14].ContentStart, tableCellCollection[14].ContentEnd).Text;
			Assert.AreEqual(String.Empty, ValueCell);
		}

		[Test]
		public void Recalculate_waybill()
		{
			var waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(24.8, waybillLine.RetailCost);
			Assert.AreEqual(Rounding.To0_10, model.Rounding.Value);
			model.Rounding.Value = Rounding.None;
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
			settings.Markups.Value[0].Markup = 50;
			settings.Markups.Value[0].MaxMarkup = 50;
			var results = settings.Save().ToList();
			Close(settings);
			scheduler.AdvanceByMs(1000);
			Assert.AreEqual("", manager.MessageBoxes.Implode());
			waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(30.8, waybillLine.RetailCost);
		}

		[Test]
		public void Print_waybill()
		{
			var results = model.PrintWaybill().GetEnumerator();
			var dialog = Next<DialogResult>(results);
			var settings = ((SimpleSettings)dialog.Model);
			Assert.That(settings.Properties.Count(), Is.GreaterThan(0));
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_PrintAct()
		{
			var results = model.PrintAct().GetEnumerator();
			var dialog = Next<DialogResult>(results);
			var settings = ((SimpleSettings)dialog.Model);
			Assert.That(settings.Properties.Count(), Is.GreaterThan(0));
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Print_racking_map()
		{
			var result = (DialogResult)model.PrintRackingMap();
			var preview = ((PrintPreviewViewModel)result.Model);
			Assert.IsNotNull(preview);
		}

		[Test]
		public void Print_price_tags()
		{
			var result = (DialogResult)model.PrintPriceTags();
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
		public void Print_registry()
		{
			var results = model.PrintRegistry().GetEnumerator();
			var dialog = Next<DialogResult>(results);
			var settings = ((SimpleSettings)dialog.Model);
			Assert.That(settings.Properties.Count(), Is.GreaterThan(0));
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Create_new_line()
		{
			waybill.IsCreatedByUser = true;
			var waybillLine = new WaybillLine();
			model.Lines.Value.AddNewItem(waybillLine);
			Assert.AreEqual(14, model.Waybill.Lines.Count);
			Assert.AreEqual(waybillLine.Waybill.Id, model.Waybill.Id);
		}

		[Test]
		public void Load_certificate()
		{
			var waybillFixture = Fixture<CreateWaybill>();
			var fixture = new CreateCertificate {
				Waybill = waybillFixture.Waybill
			};
			Fixture(fixture);
			var line = fixture.Line;
			var waybillId = line.Waybill.Log.Id;

			var updateResults = shell.Update().ToArray();
			model = Navigate(new WaybillDetails(waybillId));
			Assert.AreEqual(2, updateResults.Length,
				"должны были получить результат открытия файла накладной и оповещение о новой накладной {0}", updateResults.Implode());
			Assert.IsInstanceOf<DialogResult>(updateResults[0]);
			Assert.IsInstanceOf<OpenResult>(updateResults[1]);

			Env.Scheduler = ImmediateScheduler.Instance;
			Env.UiScheduler = ImmediateScheduler.Instance;
			var downloaded = model.Download(model.Lines.Value.Cast<WaybillLine>().First(l => l.Id == line.Id)).ToArray();
			Assert.AreEqual(0, downloaded.Length, downloaded.Implode());
			Assert.AreEqual($"Файл 'Сертификаты для {line.Product} серия {line.SerialNumber}' загружен", WaitNotification());
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

			model = Open(new WaybillDetails(waybill.Id));
			results = model.PrintRegistry().GetEnumerator();
			Assert.IsTrue(results.MoveNext());
			target = (RegistryDocumentSettings)((SimpleSettings)((DialogResult)results.Current).Model).Target;
			Assert.AreEqual("Член комитета №1", target.CommitteeMember1);
		}

		[Test]
		public void Export_waybill_to_excel()
		{
			var result = model.ExportWaybill();
			Assert.IsInstanceOf(typeof(OpenResult), result);
			Assert.IsTrue(File.Exists((result as OpenResult).Filename));
		}

		[Test]
		public void Export_waybill_to_excel_restored_ver()
		{
			var result = model.RestoredExportWaybill();
			Assert.IsInstanceOf(typeof(OpenResult), result);
			Assert.IsTrue(File.Exists((result as OpenResult).Filename));
		}
	}
}