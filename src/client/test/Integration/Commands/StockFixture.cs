using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Service.Test.TestHelpers;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class StockFixture : MixedFixture
	{
		private bool restore;

		[SetUp]
		public void Setup()
		{
			restore = false;
		}

		[TearDown]
		public void TearDown()
		{
			if (restore)
				DbHelper.RestoreData(localSession);
		}

		[Test]
		public void Mark_stock_with_reject()
		{
			var reject = localSession.Query<Client.Models.Reject>().First();
			var stock = new Stock()
			{
				Cost = 50.0m,
				RetailCost = 120.0m,
				LowCost = 100.0m,
				OptCost = 110.0m,
				Product = reject.Product,
				Seria = reject.Series
			};
			localSession.Save(stock);

			var cmd = new UpdateCommand();
			cmd.Session = localSession;
			cmd.CalculateRejectsStock(settings);

			localSession.Refresh(stock);
			Assert.AreEqual(stock.RejectStatusName, "Забраковано");
			Assert.AreEqual(reject.Id, stock.RejectId);
		}

	}
}