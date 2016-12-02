using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class ImportFixture : DbFixture
	{
		[Test]
		public void Import_future_data()
		{
			var waybill = CollectionExtention.FirstOrDefault(session.Query<Waybill>(), null);
			if (waybill == null)
			{
				Fixture(new LocalWaybill());
				waybill = session.Query<Waybill>().First();
			}
			if (!waybill.IsNew)
			{
				waybill.IsNew = true;
				session.Flush();
			}
			var waybillId = waybill.Id;
			Assert.IsTrue(waybill.IsNew);

			restore = true;
			var data = new List<Tuple<string, string[]>> {
				Tuple.Create(TempFile("Users.txt", "5\ttest\t"), new[] { "Id", "NonExistsColumn" })
			};
			var cmd = InitCmd(new ImportCommand(data) {
				Strict = false
			});
			cmd.Execute();

			waybill = session.Query<Waybill>().First(r => r.Id == waybillId);
			Assert.IsTrue(waybill.IsNew);
		}
	}
}