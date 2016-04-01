using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Integration.ViewModels;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Caliburn.Micro;
using Common.Tools;
using NPOI.HSSF.UserModel;
using NUnit.Framework;
using ReactiveUI.Testing;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class BatchFixture : BaseUnitFixture
	{
		private Batch batch;
		private Offer offer;

		[SetUp]
		public void Setup()
		{
			var settings = new Settings {
				UserName = "test",
				Password = "password"
			};

			batch = new Batch();
			batch.Settings.Value = settings;
			batch.Shell = shell;
			batch.Shell.Settings.Value = settings;
		}

		[Test]
		public void Upload_file()
		{
			var address = new Address("тест") {
				Id = 100,
			};
			Activate(batch, address);
			shell.Settings.Value.LastUpdate = DateTime.Now;
			var stub = new StubRemoteCommand(UpdateResult.OK);
			UpdateCommand cmd = null;
			shell.CommandExecuting += c => {
				cmd = (UpdateCommand)c;
				return stub;
			};
			var actions = batch.Upload().GetEnumerator();
			var file = Next<OpenFileResult>(actions);
			file.Dialog.FileName = "data.txt";

			Next(actions);
			Assert.AreEqual("data.txt", cmd.BatchFile);
			Assert.AreEqual(100, cmd.AddressId);
			Assert.IsInstanceOf<Batch>(shell.ActiveItem);
		}

		/// <summary>
		/// К задаче
		/// http://redmine.analit.net/issues/30315
		/// </summary>
		[Test(Description = "Проверка сохранения в DBF файлов, где значения не влезают в строки. Не должно быть исключений.")]
		public void DBFSaveTest()
		{
			batch.Lines.Value = new ObservableCollection<BatchLineView> {
				new BatchLineView(new BatchLine(), new OrderLine { Code = "normal", ProductSynonym = "Папаверин" }),
				new BatchLineView(new BatchLine(), new OrderLine {
					Code = "SuperLongCodeIsMoreThan9Symbols"
						+ "BetterToAddMoreSymbolsForClearTest"
						+ "MaybeItsStillNotLongEnought"
						+ "SomeMoreText" })
			};

			var results = batch.Save().GetEnumerator();
			var save = Next<SaveFileResult>(results);
			save.Dialog.FilterIndex = 1;
			var file = cleaner.RandomFile();
			save.Dialog.FileName = file;
			Next(results);

			var dbf = Dbf.Load(file, Encoding.GetEncoding(1251));
			Assert.That(dbf.Rows.Count, Is.EqualTo(2));
			Assert.AreEqual("Папаверин", dbf.Rows[0]["Name"]);
		}

		[Test]
		public void Save()
		{
			var order = new Order(new Address("тест"), new Offer(new Price("тест"), 100));
			batch.Lines.Value = new ObservableCollection<BatchLineView> {
				new BatchLineView(new BatchLine(), null),
				new BatchLineView(new BatchLine(), order.Lines[0])
			};

			var results = batch.Save().GetEnumerator();
			var save = Next<SaveFileResult>(results);
			save.Dialog.FilterIndex = 4;
			var file = save.Dialog.FileName = cleaner.RandomFile();
			Next(results);
			var text = File.ReadAllText(file, Encoding.Default);
			Assert.That(text, Does.Contain("Наименование;Производитель;Прайс-лист;Цена;Заказ;Сумма;Комментарий"));
		}

		[Test]
		public void Export_excel()
		{
			var offer = new Offer(new Price("тест"), 100) {
				ProductSynonym = "Папаверин",
				ProducerSynonym = "Биосинтез ОАО",
			};
			var order = new Order(new Address("тест"), offer);
			batch.Lines.Value = new ObservableCollection<BatchLineView> {
				new BatchLineView(new BatchLine(), null),
				new BatchLineView(new BatchLine(), order.Lines[0])
			};
			var results = batch.Save().GetEnumerator();
			var save = Next<SaveFileResult>(results);
			save.Dialog.FilterIndex = 3;
			var file = save.Dialog.FileName = cleaner.RandomFile();
			Next(results);

			using(var stream = File.OpenRead(file)) {
				var book = new HSSFWorkbook(stream);
				Assert.AreEqual(1, book.NumberOfSheets);
				var sheet = book.GetSheetAt(0);
				var row = sheet.GetRow(2);
				Assert.AreEqual("Папаверин", row.GetCell(0).StringCellValue);
				Assert.AreEqual("Биосинтез ОАО", row.GetCell(1).StringCellValue);
				Assert.AreEqual("тест", row.GetCell(2).StringCellValue);
				Assert.AreEqual(100, row.GetCell(3).NumericCellValue);
				Assert.AreEqual(1, row.GetCell(4).NumericCellValue);
				Assert.AreEqual(100, row.GetCell(5).NumericCellValue);
			}
		}

		[Test]
		public void Export_service_fields()
		{
			batch.Lines.Value = new ObservableCollection<BatchLineView> {
				new BatchLineView(new BatchLine {
					ServiceFields = @"{""f1"":""f1-value""}"
				}, null)
			};
			var writer = new StringWriter();
			batch.ExportCsv(writer, batch.Lines.Value);
			Assert.AreEqual("Наименование;Производитель;Прайс-лист;Цена;Заказ;Сумма;Комментарий;f1\r\n" +
				"\"\";\"\";\"\";\"\";\"0\";\"\";\"\";\"f1-value\"\r\n", writer.ToString());
		}

		[Test]
		public void Mark_line()
		{
			var order = MakeOrderLine().Order;
			Activate(batch, order.Address);
			order.Frozen = true;
			batch.Lines.Value = new ObservableCollection<BatchLineView> {
				new BatchLineView(new BatchLine {
					ProductId = 105,
					Address = batch.Address
				}, null),
			};
			BatchLine.CalculateStyle(batch.Address, batch.Addresses, batch.Lines.Value);
			Assert.IsTrue(batch.Lines.Value[0].ExistsInFreezed);
			Assert.IsTrue(batch.Lines.Value[0].IsCurrentAddress);
		}

		[Test]
		public void Connect_order_line()
		{
			var line = MakeOrderLine();
			line.ExportId = 45;

			Activate(batch, line.Order.Address);
			batch.BuildLineViews(new List<BatchLine> { new BatchLine(line) });
			Assert.AreEqual(line, batch.ReportLines.Value[0].OrderLine);
		}

		[Test]
		public void Update_order_stat()
		{
			var address = new Address("тест");
			InitAddress(address);
			var order = new Order(address, new Offer(new Price("тест"), 100));
			order.Lines[0].ExportId = 203;
			address.Orders.Add(order);

			Stat lastStat = null;
			bus.Listen<Stat>().Subscribe(s => lastStat = s);
			Activate(batch, address);
			batch.BuildLineViews(new List<BatchLine> { new BatchLine(order.Lines[0]) });

			batch.SelectedReportLines.Add(batch.Lines.Value[0]);
			batch.CurrentReportLine.Value = batch.Lines.Value[0];
			batch.Delete();
			scheduler.AdvanceByMs(1000);
			Assert.IsNotNull(lastStat);
			Assert.AreEqual(0, lastStat.OrdersCount);
		}

		[Test]
		public void Edit_order_multi_address()
		{
			var address1 = new Address("тест1");
			var address2 = new Address("тест2");
			InitAddress(address1, address2);
			Activate(batch, address1, address2);

			var offer = new Offer(new Price("тест"), 50);
			var line = address2.Order(offer, 5);
			batch.BuildLineViews(new List<BatchLine> { new BatchLine { Address = address1 }, new BatchLine(line) });

			batch.CurrentReportLine.Value = batch.Lines.Value[1];
			batch.Offers.Value = new List<Offer> {
				offer
			};
			batch.Update();
			scheduler.AdvanceByMs(1000);

			batch.CurrentOffer.Value = batch.Offers.Value[0];
			Assert.AreEqual(5, batch.CurrentOffer.Value.OrderCount);
			Order(0);
			Order(10);

			Assert.AreEqual(0, address1.Orders.Count);
			Assert.AreEqual(1, address2.Orders.Count);
			Assert.AreEqual(500, address2.Orders[0].Sum);
		}

		[Test]
		public void Load_order_lines()
		{
			var address1 = new Address("тест1");
			var address2 = new Address("тест2");
			InitAddress(address1, address2);

			var offer = new Offer(new Price("тест"), 50);
			var line = address2.Order(offer, 5);
			line.ExportId = 562;
			Activate(batch, address1, address2);
			batch.BuildLineViews(new List<BatchLine> { new BatchLine(line) });

			Assert.IsTrue(batch.CanReload);
			Assert.IsTrue(batch.CanClear);
			Assert.AreEqual(line, batch.ReportLines.Value[0].OrderLine);
		}

		[Test]
		public void Delete_batch_line_on_delete_order_line()
		{
			var address = new Address("тест");
			InitAddress(address);
			var offer = new Offer(new Price("тест"), 50);
			var line = address.Order(offer, 5);
			line.ExportId = 562;
			Activate(batch, address);
			batch.BuildLineViews(new List<BatchLine> { new BatchLine(line) });

			batch.CurrentReportLine.Value = batch.Lines.Value[0];
			batch.Offers.Value = new List<Offer> {
				offer,
			};
			batch.Update();
			batch.CurrentOffer.Value = batch.Offers.Value.First();
			Assert.AreEqual(5, batch.CurrentOffer.Value.OrderCount);
			Order(0);
			Assert.AreEqual(0, batch.ReportLines.Value.Count);
			Assert.AreEqual(0, batch.Lines.Value.Count);
		}

		[Test]
		public void Filter_lines()
		{
			var address = new Address("тест");
			InitAddress(address);
			var offer = new Offer(new Price("тест"), 50);
			var line = address.Order(offer, 5);
			line.ExportId = 562;
			Activate(batch, address);
			batch.BuildLineViews(new List<BatchLine> { new BatchLine(line), new BatchLine(new Catalog("тест"), address) });

			//заказано
			batch.CurrentFilter.Value = batch.Filter[1];
			Assert.AreEqual(1, batch.ReportLines.Value.Count);
			Assert.IsNotNull(batch.ReportLines.Value[0].OrderLine);
		}

		[Test]
		public void Reset_search_term_on_filter()
		{
			batch.SearchBehavior.SearchText.Value = "тест";
			batch.SearchBehavior.Search();
			Assert.AreEqual("тест", batch.SearchBehavior.ActiveSearchTerm.Value);
			batch.CurrentFilter.Value = "Не заказано";
			Assert.AreEqual("", batch.SearchBehavior.ActiveSearchTerm.Value);
		}

		[Test]
		public void Delete_line_on_zero_edit()
		{
			var line = MakeOrderLine();
			Activate(batch, line.Order.Address);
			batch.BuildLineViews(new List<BatchLine> { new BatchLine(line) });
			batch.CurrentReportLine.Value = batch.ReportLines.Value.First();
			batch.SelectedReportLines.Add(batch.CurrentReportLine.Value);
			batch.CurrentReportLine.Value.Value = 0;
			batch.ReportEditor.Updated();
			Assert.AreEqual(0, batch.ReportLines.Value.Count);
		}

		[Test]
		public void Show_report_for_manually_ordered_lines()
		{
			var line = MakeOrderLine();
			Activate(batch, line.Order.Address);
			batch.BuildLineViews(new List<BatchLine>());
			Assert.AreEqual(1, batch.ReportLines.Value.Count);
			var report = batch.ReportLines.Value[0];
			Assert.IsTrue(report.IsCurrentAddress);
			Assert.IsFalse(report.IsNotOrdered);
			Assert.IsNull(report.BatchLine);
			Assert.IsNotNull(report.OrderLine);
		}

		[Test]
		public void Delete_report_line_on_delete_order_line()
		{
			var line = MakeOrderLine();
			Activate(batch, line.Order.Address);
			batch.BuildLineViews(new List<BatchLine>());

			batch.CurrentReportLine.Value = batch.Lines.Value[0];
			batch.Offers.Value = new List<Offer> {
				offer,
			};
			batch.Update();
			batch.CurrentOffer.Value = batch.Offers.Value.First();
			Assert.AreEqual(1, batch.CurrentOffer.Value.OrderCount);
			Order(0);
			Assert.AreEqual(0, batch.ReportLines.Value.Count);
			Assert.AreEqual(0, batch.Lines.Value.Count);
		}

		[Test]
		public void Show_report_line_on_order()
		{
			var line = MakeOrderLine();
			Activate(batch, line.Order.Address);
			batch.BuildLineViews(new List<BatchLine>());

			batch.CurrentReportLine.Value = batch.Lines.Value[0];
			batch.Offers.Value = new List<Offer> {
				offer,
				new Offer(offer, 150)
			};
			batch.Update();
			batch.CurrentOffer.Value = batch.Offers.Value.First(x => x.OrderCount == null);
			Order(1);
			Assert.AreEqual(2, batch.ReportLines.Value.Count);
			Assert.AreEqual(2, batch.Lines.Value.Count);
			Order(0);
			Assert.AreEqual(1, batch.ReportLines.Value.Count);
			Assert.AreEqual(1, batch.Lines.Value.Count);
		}

		private void Order(uint count)
		{
			batch.CurrentOffer.Value.OrderCount = count;
			batch.OfferUpdated();
			batch.OfferCommitted();
		}

		private void InitAddress(params Address[] addresses)
		{
			for(var i = 0; i < addresses.Length; i++) {
				addresses[i].Id = (uint)i;
			}
		}

		private OrderLine MakeOrderLine()
		{
			var address = new Address("тест");
			InitAddress(address);
			offer = new Offer(new Price("тест"), 100) {
				ProductSynonym = "Тестовый товар",
				ProducerSynonym = "Тестовый производитель",
				ProductId = 105,
			};
			return address.Order(offer, 1);
		}

		private void Next(IEnumerator<IResult> results)
		{
			results.MoveNext();
		}

		private T Next<T>(IEnumerator<IResult> results)
		{
			Assert.IsTrue(results.MoveNext());
			Assert.IsInstanceOf<T>(results.Current);
			return (T)results.Current;
		}
	}
}