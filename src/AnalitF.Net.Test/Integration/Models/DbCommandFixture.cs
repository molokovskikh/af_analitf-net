using System.IO;
using System.Linq;
using System.Threading;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	public class DbCommandFixture : DbFixture
	{
		[TearDown]
		public void TearDown()
		{
			restore = true;
		}

		[Test]
		public void Repair_data_base()
		{
			Directory.GetFiles("data", "mnns.*").Each(File.Delete);
			File.WriteAllBytes(Path.Combine("data", "markupconfigs.frm"), new byte[0]);

			var command = InitCmd(new RepairDb());
			command.Execute();
			var result = command.Result;

			Assert.That(result, Is.False);
			Assert.That(Directory.GetFiles("data", "mnns.*").Length, Is.EqualTo(3));
			Assert.That(new FileInfo(Path.Combine("data", "markupconfigs.frm")).Length, Is.GreaterThan(0));
		}

		[Test]
		public void Clean_db()
		{
			var command = InitCmd(new CleanDb());
			command.Execute();

			Assert.That(session.Query<Offer>().Count(), Is.EqualTo(0));
			Assert.That(session.Query<Settings>().Count(), Is.EqualTo(1));
		}
	}
}