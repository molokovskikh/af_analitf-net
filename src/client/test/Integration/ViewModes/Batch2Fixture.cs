using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class Batch2Fixture : ViewModelFixture<Batch2>
	{
		[Test]
		public void Delete_lines()
		{
			session.DeleteEach<BatchLine>();
			session.DeleteEach<Order>();

			var lines = session.Query<Offer>().Take(2).Select(o => address.Order(o, 1))
				.Select(l => new BatchLine(l))
				.ToArray();
			session.SaveEach(lines);

			var batchLines = model.BatchLines;
			testScheduler.Start();
			model.CurrentBatchLine.Value = batchLines.Value.FirstOrDefault();
			model.SelectedBatchLines.AddEach(batchLines.Value.Take(2));
			Assert.IsTrue(model.CanDelete);
			model.Delete();
			Close(model);
			session.Clear();
			Assert.AreEqual(0, session.Query<Order>().Count());
		}
	}
}