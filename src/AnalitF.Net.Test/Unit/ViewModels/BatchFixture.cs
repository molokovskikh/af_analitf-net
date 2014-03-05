using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Test.Integration.ViewModes;
using Caliburn.Micro;
using Microsoft.Win32;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using ReactiveUI.Testing;
using Xceed.Wpf.Toolkit;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class BatchFixture : BaseUnitFixture
	{
		private Batch batch;
		private ShellViewModel shell;

		[SetUp]
		public void Setup()
		{
			var settings = new Settings {
				UserName = "test",
				Password = "password"
			};

			batch = new Batch();
			shell = new ShellViewModel(true);
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
			batch.Address = address;
			shell.CurrentAddress = address;
			shell.ActiveItem = batch;
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

		[Test]
		public void Save()
		{
			var order = new Order(new Address("тест"), new Offer(new Price("тест"), 100));
			batch.Lines = new List<BatchLine> {
				new BatchLine(),
				new BatchLine {
					Line = order.Lines[0]
				}
			};

			var results = batch.Save().GetEnumerator();
			var save = Next<SaveFileResult>(results);
			save.Dialog.FilterIndex = 4;
			var file = RandomFile();
			save.Dialog.FileName = file;
			Next(results);
			var text = File.ReadAllText(file, Encoding.Default);
			Assert.That(text, Is.StringContaining("Наименование;Производитель;Прайс-лист;Цена;Заказ;Сумма;Комментарий"));
		}

		[Test]
		public void Export_excel()
		{
			var order = new Order(new Address("тест"), new Offer(new Price("тест"), 100));
			batch.Lines = new List<BatchLine> {
				new BatchLine(),
				new BatchLine {
					Line = order.Lines[0]
				}
			};
			var results = batch.Save().GetEnumerator();
			var save = Next<SaveFileResult>(results);
			save.Dialog.FilterIndex = 3;
			var file = RandomFile();
			save.Dialog.FileName = file;
			Next(results);

			using(var stream = File.OpenRead(file)) {
				var book = new HSSFWorkbook(stream);
				Assert.AreEqual(1, book.NumberOfSheets);
			}
		}

		[Test]
		public void Export_service_fields()
		{
			batch.Lines = new List<BatchLine> {
				new BatchLine {
					ServiceFields = @"{""f1"":""f1-value""}"
				}
			};
			var writer = new StringWriter();
			batch.ExportCsv(writer);
			Assert.AreEqual("Наименование;Производитель;Прайс-лист;Цена;Заказ;Сумма;Комментарий;f1\r\n" +
				"\"\";\"\";\"\";\"\";\"0\";\"\";\"\";\"f1-value\"\r\n", writer.ToString());
		}

		[Test]
		public void Mark_line()
		{
			var order = MakeOrderLine().Order;
			order.Frozen = true;
			batch.Lines = new List<BatchLine> {
				new BatchLine {
					ProductId = 105,
					Address = batch.Address
				},
			};
			batch.CalculateStatus();
			Assert.IsTrue(batch.Lines[0].ExistsInFreezed);
		}

		[Test]
		public void Connect_order_line()
		{
			var line = MakeOrderLine();
			line.ExportId = 45;

			batch.Lines.Add(new BatchLine(line));
			ScreenExtensions.TryActivate(batch);
			Assert.AreEqual(line, batch.ReportLines.Value[0].Line);
		}

		[Test]
		public void Update_order_stat()
		{
			var address = new Address("тест");
			InitAddress(address);
			var order = new Order(address, new Offer(new Price("тест"), 100));
			order.Lines[0].ExportId = 203;
			address.Orders.Add(order);

			batch.Address = address;
			Stat lastStat = null;
			bus.Listen<Stat>().Subscribe(s => lastStat = s);
			batch.Lines = new List<BatchLine> {
				new BatchLine(order.Lines[0])
			};
			ScreenExtensions.TryActivate(batch);

			batch.CurrentReportLine.Value = batch.Lines[0];
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
			ScreenExtensions.TryActivate(batch);

			batch.Lines.Add(new BatchLine { Address = address1 });

			var offer = new Offer(new Price("тест"), 50);
			var line = address2.Order(offer, 5);
			batch.Lines.Add(new BatchLine(line));

			batch.CurrentReportLine.Value = batch.Lines[1];
			batch.Offers.Value = new List<Offer> {
				offer
			};
			batch.Update();

			batch.CurrentOffer.Value = batch.Offers.Value[0];
			Assert.AreEqual(5, batch.CurrentOffer.Value.OrderCount);
			batch.CurrentOffer.Value.OrderCount = 0;
			batch.OfferUpdated();
			batch.OfferCommitted();
			batch.CurrentOffer.Value.OrderCount = 10;
			batch.OfferUpdated();
			batch.OfferCommitted();

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
			batch.Lines.Add(new BatchLine(line));
			ScreenExtensions.TryActivate(batch);

			Assert.AreEqual(line, batch.ReportLines.Value[0].Line);
		}

		[Test]
		public void Delete_batch_line_on_delete_order_line()
		{
			var address = new Address("тест");
			InitAddress(address);
			var offer = new Offer(new Price("тест"), 50);
			var line = address.Order(offer, 5);
			line.ExportId = 562;
			batch.Lines.Add(new BatchLine(line));
			ScreenExtensions.TryActivate(batch);

			batch.CurrentReportLine.Value = batch.Lines[0];
			batch.Offers.Value = new List<Offer> {
				offer,
			};
			batch.Update();
			batch.CurrentOffer.Value = batch.Offers.Value.First();
			Assert.AreEqual(5, batch.CurrentOffer.Value.OrderCount);
			batch.CurrentOffer.Value.OrderCount = 0;
			batch.OfferUpdated();
			batch.OfferCommitted();
			Assert.AreEqual(0, batch.ReportLines.Value.Count);
			Assert.AreEqual(0, batch.Lines.Count);
		}

		[Test]
		public void Filter_lines()
		{
			var address = new Address("тест");
			InitAddress(address);

			var offer = new Offer(new Price("тест"), 50);
			var line = address.Order(offer, 5);
			line.ExportId = 562;
			batch.Lines.Add(new BatchLine(line));
			batch.Lines.Add(new BatchLine(new Catalog("тест"), address));
			ScreenExtensions.TryActivate(batch);

			//заказано
			batch.CurrentFilter.Value = batch.Filter[1];
			Assert.AreEqual(1, batch.ReportLines.Value.Count);
			Assert.IsNotNull(batch.ReportLines.Value[0].Line);
		}

		private void InitAddress(params Address[] addresses)
		{
			for(var i = 0; i < addresses.Length; i++) {
				addresses[i].Id = (uint)i;
			}
			batch.Addresses = addresses;
			batch.Address = addresses.FirstOrDefault();
			shell.Addresses = addresses.ToList();
			shell.CurrentAddress = addresses.FirstOrDefault();
		}

		private OrderLine MakeOrderLine()
		{
			var address = new Address("тест");
			InitAddress(address);
			batch.Address = address;
			var offer = new Offer(new Price("тест"), 100) {
				ProductId = 105
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