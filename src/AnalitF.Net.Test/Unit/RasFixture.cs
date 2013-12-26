using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Devart.Data.MySql;
using DotRas;
using NPOI.HSSF.Record.Chart;
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

		[Test]
		public void Ras_download()
		{
			using (var client = RasHttp()) {
				var t = client.GetAsync("http://google.com");
				Safe(t.Wait);
				Assert.AreEqual(1, RasConnection.GetActiveConnections().Count);
			}
			Assert.AreEqual(0, RasConnection.GetActiveConnections().Count);
		}

		[Test]
		public void Concurent_connections()
		{
			using (var disposable = new CompositeDisposable()) {
				var client1 = RasHttp();
				disposable.Add(client1);
				var r1 = client1.GetAsync("http://google.com");
				Safe(r1.Wait);

				var client2 = RasHttp();
				disposable.Add(client2);
				var r2 = client2.GetAsync("http://google.com");

				client1.Dispose();
				Assert.AreEqual(1, RasConnection.GetActiveConnections().Count);
				client2.Dispose();
				Assert.AreEqual(0, RasConnection.GetActiveConnections().Count);
			}
		}

		public HttpClient RasHttp()
		{
			var handler = new HttpClientHandler();
			var progress = new ProgressMessageHandler();
			var ras = new RasHandler(EntryName);
			return HttpClientFactory.Create(handler, ras, progress);
		}

		public Exception Safe(Action action)
		{
			try {
				action();
			}
			catch(Exception e) {
				return e;
			}
			return null;
		}
	}
}