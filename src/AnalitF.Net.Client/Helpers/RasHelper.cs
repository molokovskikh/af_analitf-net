using System;
using System.Linq;
using Common.Tools;
using DotRas;

namespace AnalitF.Net.Client.Helpers
{
	public class RasHelper : IDisposable
	{
		private string connectionName;
		private RasConnection connection;

		public RasHelper(string connection)
		{
			this.connectionName = connection;
		}

		public void Open()
		{
			if (String.IsNullOrEmpty(connectionName))
				return;

			if (RasConnection.GetActiveConnections().Any(c => c.EntryName == connectionName))
				return;

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
				connection = RasConnection.GetActiveConnections().FirstOrDefault(c => c.Handle == handle);
			}
		}

		public void Dispose()
		{
			if (connection != null)
				connection.HangUp();
		}
	}
}