using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows.Documents;
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
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;

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
			var waybillLines = waybill.Lines;

			/* проверка надбавки в рублях в первой препарате */
			Assert.AreEqual(4m, waybillLines[0].RetailMarkupInRubles);

			var doc = new RegistryDocument(waybill, waybillLines);
			var flowDoc = doc.Build();

			var listTableCellCollection = flowDoc.Blocks.OfType<Table>().Skip(1).First()
					.RowGroups.Select(x => x.Rows).ToList()
					.First().Select(x => x.Cells).ToList();

			var tableCellCollection = listTableCellCollection[0];

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
			Assert.AreEqual(string.Empty, ValueCell);
		}

		[Test]
		public void Recalculate_waybill()
		{
			var waybillLine = model.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(24.8, waybillLine.RetailCost);
			Assert.AreEqual(Rounding.To0_10, model.Waybill.Rounding);
			model.Waybill.Rounding = Rounding.None;
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
			var settings = (SimpleSettings) dialog.Model;
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
		public void Print_PrintProtocol()
		{
			var results = model.PrintProtocol().GetEnumerator();
			var dialog = Next<DialogResult>(results);
			var settings = ((SimpleSettings)dialog.Model);
			Assert.That(settings.Properties.Count(), Is.GreaterThan(0));
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		[Test]
		public void Set_productname_from_catalog()
		{
			var catalog = session.Query<Catalog>().First();
			var line = waybill.Lines[10];
			line.CatalogId = catalog.Id;
			var stock = new Stock(waybill, line, session);
			session.Save(stock);
			Assert.AreEqual(stock.Product, catalog.FullName);
		}

		[Test]
		public void Consumption_report()
		{
			var line = waybill.Lines[10];
			line.CatalogId = null;
			var stock = new Stock(waybill, line, session);
			session.Save(stock);
			Assert.AreEqual(stock.Product, line.Product);

			var check = new Check();
			check.Status = Status.Closed;
			session.Save(check);

			var checkLine = new CheckLine(stock, 1, CheckType.SaleBuyer);
			checkLine.CheckId = check.Id;
			session.Save(checkLine);
			session.Flush();

			var result = model.ConsumptionReport().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();
			var open = Next<OpenResult>(result);
			Assert.IsTrue(File.Exists(open.Filename), open.Filename);
			Assert.That(open.Filename, Does.Contain("Расход по документу"));
		}

		[Test]
		public void Print_invoice()
		{
			var result = (DialogResult) model.PrintInvoice().First();
			var preview = (PrintPreviewViewModel) result.Model;
			Assert.IsNotNull(preview.Document);
		}

		[Test]
		public void Edit_sum()
		{
			var result = (DialogResult)model.EditSum().First();
			var simpleSettings = (SimpleSettings)result.Model;
			Assert.IsNotNull(simpleSettings.Target);
			Assert.IsInstanceOf<EditSumSettings>(simpleSettings.Target);
		}

		[Test]
		public void Print_registry()
		{
			var results = model.PrintRegistry().GetEnumerator();
			var dialog = Next<DialogResult>(results);
			var settings = (RegistryDocSettings) dialog.Model;
			Assert.IsNotNull(settings);
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
				"должны были получить результат открытия файла накладной и оповещение о новой накладной {0}",
				updateResults.Implode());
			Assert.IsInstanceOf<DialogResult>(updateResults[0]);
			Assert.IsInstanceOf<OpenResult>(updateResults[1]);

			Env.Scheduler = ImmediateScheduler.Instance;
			Env.UiScheduler = ImmediateScheduler.Instance;
			var downloaded = model.Download(model.Lines.Value.Cast<WaybillLine>().First(l => l.Id == line.Id)).ToArray();
			Assert.AreEqual(0, downloaded.Length, downloaded.Implode());
			Assert.AreEqual($"Файл 'Сертификаты для {line.Product} серия {line.SerialNumber}' загружен", WaitNotification());
		}

		[Test]
		public void Persist_registry_config()
		{
			var results = model.PrintRegistry().GetEnumerator();
			Assert.IsTrue(results.MoveNext());
			var target = (RegistryDocSettings) ((DialogResult) results.Current).Model;
			target.CommitteeMember1 = "Член комитета №1";
			target.OK();
			Assert.IsTrue(results.MoveNext());
			Assert.IsNotNull(results.Current);
			Close(model);

			model = Open(new WaybillDetails(waybill.Id));
			results = model.PrintRegistry().GetEnumerator();
			Assert.IsTrue(results.MoveNext());
			target = (RegistryDocSettings) ((DialogResult) results.Current).Model;
			Assert.AreEqual("Член комитета №1", target.CommitteeMember1);
		}

		[Test]
		public void Export_waybill_to_excel()
		{
			var result = model.ExportWaybill();
			Assert.IsInstanceOf(typeof (OpenResult), result);
			Assert.IsTrue(File.Exists((result as OpenResult).Filename));
		}

		[Test]
		public void Export_waybill_to_excel_restored_ver()
		{
			var result = model.RestoredExportWaybill();
			Assert.IsInstanceOf(typeof (OpenResult), result);
			Assert.IsTrue(File.Exists((result as OpenResult).Filename));
		}

		[Test]
		public void Mark_as_read()
		{
			var id = model.Waybill.Id;
			model.Waybill.IsNew = true;
			model.Session.Flush();
			Assert.IsTrue(model.Waybill.IsNew);
			Close(model);
			model = Open(new WaybillDetails(id));
			Assert.IsTrue(!model.Waybill.IsNew);
		}

		[Test]
		public void Waybill_to_editable()
		{
			model.User.IsStockEnabled = true;
			model.Waybill.IsCreatedByUser = false;
			Assert.IsTrue(model.Waybill.IsReadOnly);
			model.ToEditable();
			Assert.IsTrue(model.Waybill.IsReadOnly);
		}

		[Test]
		public void Waybill_posted_to_editable()
		{
			model.User.IsStockEnabled = true;
			model.Waybill.IsCreatedByUser = false;
			Assert.IsTrue(model.Waybill.IsReadOnly);
			model.ToEditable();
			Assert.IsTrue(model.Waybill.IsReadOnly);
			model.Waybill.Status = DocStatus.Posted;
			Assert.IsTrue(model.Waybill.IsReadOnly);
			model.ToEditable();
			Assert.IsTrue(model.Waybill.IsReadOnly);
		}
	}
}