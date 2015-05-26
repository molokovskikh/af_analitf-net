using System;
using System.Linq;
using System.Threading;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools.Calendar;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class WaybillLineSearchFixture : ViewModelFixture
	{
		private Supplier supplier;
		private Waybill waybill;

		[SetUp]
		public void Setup()
		{
			supplier = session.Query<Supplier>().First();
			waybill = new Waybill(address, supplier);
			var line = new WaybillLine(waybill) {
				Product = "АКРИДЕРМ ГК 15,0 МАЗЬ",
				Certificates = "РОСС RU.ФМ01.Д19782",
				ProducerCost = 258,
				Nds = 10,
				SupplierCostWithoutNds = 258,
				SupplierCost = 283.8m,
				Quantity = 2,
				SerialNumber = "120214"
			};
			waybill.AddLine(line);
			session.Save(waybill);
		}

		[Test]
		public void Save_not_found_state()
		{
			Env.Barrier = new Barrier(2);
			supplier.HaveCertificates = true;

			var line = waybill.Lines[0];
			var model = Init(new WaybillLineSearch(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(+1)));
			testScheduler.Start();
			var waybillLine = model.Lines.Value.First(l => l.Id == line.Id);
			var results = model.Download(waybillLine).ToArray();
			Assert.AreEqual(0, results.Length);
			Assert.IsTrue(Env.Barrier.SignalAndWait(10.Second()), "не удалось дождаться загрузки");
			testScheduler.Start();
			Close(model);

			session.Refresh(line);
			Assert.IsTrue(line.IsCertificateNotFound);
			Assert.AreEqual(0, shell.PendingDownloads.Count);
		}

		[Test]
		public void Display_download_state_after_reload()
		{
			Env.Barrier = new Barrier(2);
			supplier.HaveCertificates = true;

			var model = Init(new WaybillLineSearch(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(+1)));
			testScheduler.Start();
			var line = waybill.Lines[0];
			var waybillLine = model.Lines.Value.First(l => l.Id == line.Id);
			var results = model.Download(waybillLine).ToArray();
			Assert.AreEqual(0, results.Length);
			testScheduler.Start();

			Close(model);

			model = Init(new WaybillLineSearch(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(+1)));
			testScheduler.Start();
			waybillLine = model.Lines.Value.First(l => l.Id == line.Id);
			Assert.IsTrue(waybillLine.IsDownloading);
		}

		[Test]
		public void Enter_line()
		{
			var model = Init(new WaybillLineSearch(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(+1)));
			testScheduler.Start();
			model.CurrentLine.Value = model.Lines.Value.First();
			Assert.IsTrue(model.CanEnterLine.Value);
			model.EnterLine();
			Assert.IsInstanceOf<WaybillDetails>(shell.ActiveItem);
		}
	}
}