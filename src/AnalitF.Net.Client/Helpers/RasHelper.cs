using System;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Common.Tools;
using DotRas;

namespace AnalitF.Net.Client.Helpers
{
	public class RasHandler : DelegatingHandler
	{
		private RasHelper rasHelper;

		public RasHandler(string connectionname)
		{
			rasHelper = new RasHelper(connectionname);
		}

		protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var task = new Task(() => rasHelper.Open());
			task.Start();
			await task;
			return await base.SendAsync(request, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			if (rasHelper != null)
				rasHelper.Dispose();
			base.Dispose(disposing);
		}
	}

	public class RasHelper : IDisposable
	{
		private string connectionName;

		private static object sync = new object();
		private static RefCountDisposable connection;
		private IDisposable connectionRef = Disposable.Empty;

		public RasHelper(string connection)
		{
			connectionName = connection;
		}

		public void Open()
		{
			if (String.IsNullOrEmpty(connectionName))
				return;

			lock(sync) {
				//если соединение есть и его открыли мы тогда берем ссылку
				if (connection != null && !connection.IsDisposed) {
					connectionRef = connection.GetDisposable();
					return;
				}

				//если соединение открыли не мы, тогда выходим
				if (RasConnection.GetActiveConnections().Any(c => c.EntryName == connectionName)) {
					return;
				}

				string phonebookPath = null;
				var paths = new[] {
					RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User),
					RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.AllUsers)
				};
				foreach (var path in paths) {
					using(var book = new RasPhoneBook()) {
						book.Open(path);
						if (book.Entries.Any(e => e.Name.Match(connectionName)))
							phonebookPath = book.Path;
					}
				}

				if (phonebookPath == null)
					return;

				using(var dialer = new RasDialer()) {
					dialer.PhoneBookPath = phonebookPath;
					dialer.EntryName = connectionName;
					var handle = dialer.Dial();

					var rasConnection = RasConnection.GetActiveConnections().FirstOrDefault(c => c.Handle == handle);
					connection = new RefCountDisposable(Disposable.Create(() => {
						if (rasConnection != null)
							rasConnection.HangUp();
					}));
					connectionRef = connection.GetDisposable();
					connection.Dispose();
				}
			}
		}

		public void Dispose()
		{
			lock(sync)
				connectionRef.Dispose();
		}
	}
}