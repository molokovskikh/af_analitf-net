using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Common.Tools;
using DotRas;
using log4net;

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
			rasHelper?.Dispose();
			base.Dispose(disposing);
		}
	}

	public class RasHelper : IDisposable
	{
		private static readonly object sync = new object();
		private static RefCountDisposable connection;

		private ILog log = LogManager.GetLogger(typeof(RasHelper));
		private readonly string connectionName;
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
				foreach (var path in GetPhoneBooks()) {
					using(var book = new RasPhoneBook()) {
						book.Open(path);
						if (book.Entries.Any(e => e.Name.Match(connectionName)))
							phonebookPath = book.Path;
					}
				}

				if (phonebookPath == null) {
					log.Warn($"Не удалось найти соединение {connectionName}, удаленное соединение устанавливаться не будет");
					return;
				}

				using(var dialer = new RasDialer()) {
					dialer.PhoneBookPath = phonebookPath;
					dialer.EntryName = connectionName;
					var handle = dialer.Dial();

					var rasConnection = RasConnection.GetActiveConnections().FirstOrDefault(c => c.Handle == handle);
					connection = new RefCountDisposable(Disposable.Create(() => rasConnection?.HangUp()));
					connectionRef = connection.GetDisposable();
					connection.Dispose();
				}
			}
		}

		public static string[] GetPhoneBooks()
		{
			var paths = new List<string> {
				RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User),
			};
			//если пользователь не администратор и включен uac доступа к глабальной телефонной книге не будет
			try {
				var path = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.AllUsers);
				using (File.OpenRead(path)) { }
				paths.Add(path);
			}
			catch(Exception) { }
			return paths.ToArray();
		}

		public void Dispose()
		{
			lock(sync)
				connectionRef.Dispose();
		}
	}
}