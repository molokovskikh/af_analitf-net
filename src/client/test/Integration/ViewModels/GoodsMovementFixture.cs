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
using System.Reactive.Disposables;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Inventory;
using NHibernate.Linq;
using NHibernate.Util;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class GoodsMovementFixture : ViewModelFixture<GoodsMovement>
	{
		private Waybill waybill;

		[SetUp]
		public void Setup()
		{
			waybill = Fixture<LocalWaybill>().Waybill;
		}

		[Test]
		public void Goods_movement_report()
		{
			var catalog = session.Query<Catalog>().First();
			var w = session.Load<Waybill>(waybill.Id);
			var line = w.Lines[0];
			line.CatalogId = catalog.Id;
			line.Product = catalog.Name.Name;
			w.Status = DocStatus.Posted;
			session.Update(w);
			session.Flush();
			FileHelper.InitDir(settings.MapPath("Reports"));

			model.Items.Add(catalog);
			model.CurrentItem.Value = catalog;
			var result = model.ExportExcel().GetEnumerator();
			var task = Next<TaskResult>(result);
			task.Task.Start();
			task.Task.Wait();
			var open = Next<OpenResult>(result);
			Assert.IsTrue(File.Exists(open.Filename), open.Filename);
			Assert.That(open.Filename, Does.Contain("Движение товара по накладным"));
		}
	}
}