using AnalitF.Net.Client.Helpers;
using Common.Tools;
using DotRas;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture, Explicit("Тестирование предполагает наличие ras соединения")]
	public class RasFixture
	{
		private string EntryName;

		[SetUp]
		public void Setup()
		{
			var p = new RasPhoneBook();
			p.Open(RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User));
			EntryName = p.Entries[0].Name;

			RasConnection.GetActiveConnections().Each(c => c.HangUp());
		}

		[TearDown]
		public void TearDown()
		{
			RasConnection.GetActiveConnections().Each(c => c.HangUp());
		}

		[Test]
		public void Connect()
		{
			using(var helper = new RasHelper(EntryName)) {
				helper.Open();
				Assert.AreEqual(1, RasConnection.GetActiveConnections().Count);
			}
			Assert.AreEqual(0, RasConnection.GetActiveConnections().Count);
		}

		[Test]
		public void Do_not_close_not_owned_connection()
		{
			using(var dialer = new RasDialer()) {
				dialer.PhoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User);
				dialer.EntryName = EntryName;
				dialer.Dial();
			}

			using(var helper = new RasHelper(EntryName)) {
				helper.Open();
			}
			Assert.AreEqual(1, RasConnection.GetActiveConnections().Count);
		}

		[Test]
		public void Ignore_empty()
		{
			using(var helper = new RasHelper("")) {
				helper.Open();
				Assert.AreEqual(0, RasConnection.GetActiveConnections().Count);
			}
		}
	}
}