using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text.RegularExpressions;
using Common.MySql;
using Common.Tools;
using MySql.Data.MySqlClient;

namespace AnalitF.Net.Client.Test.Tasks
{
	public class Log
	{
		public class Error
		{
			public string User;
			public string Version;
			public DateTime Date;
			public string Text;
		}

		public void Execute(string version, string user, string password)
		{
			var errors = new List<Error>();
			using(var connection = new MySqlConnection(String.Format("server=sql.analit.net; user={0}; Password={1};", user, password))) {
				connection.Open();
				var records = connection.Read("select * from Logs.ClientAppLogs where version = ?version", new { version });
				foreach (var record in records) {
					var log = record["Text"].ToString();
					var reader = new StringReader(log);
					string line;
					var messageHeader = new Regex(@"^\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}\.\d{3} \[\d+\] \w+ ");
					var lastError = new Error();
					while ((line = reader.ReadLine()) != null) {
						if (messageHeader.IsMatch(line)) {
							if (lastError.User != null) {
								lastError = new Error();
								errors.Add(lastError);
							}
							lastError.User = record["UserId"].ToString();
						}
						else {
							lastError.Text += line + "\r\n";
						}
					}
				}
			}

			var groups = errors.GroupBy(e => e.Text).OrderByDescending(g => g.Count());
			foreach (var @group in groups) {
				Console.WriteLine("Count = " + @group.Count());
				Console.WriteLine("Users = " + @group.Select(g => g.User).Distinct().Implode());
				Console.WriteLine(@group.Key);
				Console.WriteLine();
			}
		}
	}
}