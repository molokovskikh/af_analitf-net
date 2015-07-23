using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	public class BatchFixture : ViewModelFixture<Batch>
	{
		[Test]
		public void Load_product_info()
		{
			session.DeleteEach<BatchLine>();

			var catalog = session.Query<Catalog>().First(c => !c.HaveOffers);
			session.Save(new BatchLine(catalog, address));

			model.CurrentReportLine.Value = model.Lines.Value.First();
			scheduler.AdvanceByMs(2000);
			Assert.IsNotNull(model.CurrentCatalog);
			Assert.AreEqual(catalog.Id, model.CurrentCatalog.Id);
		}

		[Test]
		public void Delete_line()
		{
			session.DeleteEach<BatchLine>();

			var catalog = session.Query<Catalog>().First(c => !c.HaveOffers);
			var batchLine = new BatchLine(catalog, address);
			session.Save(batchLine);

			model.SelectedReportLines.Add(model.Lines.Value.First());
			model.CurrentReportLine.Value = model.Lines.Value.First();
			Assert.IsTrue(model.CanDelete);
			model.Delete();
			Close(model);

			session.Clear();
			Assert.IsNull(session.Get<BatchLine>(batchLine.Id));
		}
	}
}