using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text.RegularExpressions;
using Common.MySql;
using Common.Tools;
using Humanizer;
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

		public class UpdateStat
		{
			public Version Version;
			public DateTime UpdateDate;
			public uint UserId;
			public long Size;
			public TimeSpan Time;
			public string Type;
		}

		public void ImportStat(string user, string password, DateTime begin)
		{
			var stats = new List<UpdateStat>();
			var updateToken = "Запрос обновления, тип обновления";
			var downloadToken = "AnalitF.Net.Client.Models.Commands.UpdateCommand - Обновление загружено, размер";
			var impotToken = "AnalitF.Net.Client.Models.Commands.UpdateCommand - Обновление завершено успешно";
			if (begin == DateTime.MinValue) {
				begin = DateTime.Today.AddDays(-14);
			}

			using(var connection = new MySqlConnection(String.Format("server=sql.analit.net; user={0}; Password={1};", user, password))) {
				connection.Open();
				var sql = String.Format("select * from Logs.ClientAppLogs where CreatedOn > ?begin and userId <> 758");
				var records = connection.Read(sql, new { begin, });

				foreach (var log in records) {
						var text = log["Text"].ToString();
						var reader = new StringReader(text);
					string line;

					var type = "";
					var beginFound = false;
					var updateBegin = DateTime.MinValue;
					long size = 0;
					while ((line = reader.ReadLine()) != null) {
						if (line.IndexOf(updateToken) >= 0) {
							type = Regex.Match(line, @"тип обновления\s'([\w\s]+)'", RegexOptions.IgnoreCase).Groups[1].Value;
							beginFound = true;
							continue;
						}
						if (beginFound && line.IndexOf(downloadToken, StringComparison.CurrentCultureIgnoreCase) >= 0) {
							updateBegin = DateTime.Parse(line.Substring(0, 24));
							size = long.Parse(Regex.Match(line, @"размер\s(\d+)", RegexOptions.IgnoreCase).Groups[1].Value);
							continue;
						}
						if (beginFound && line.IndexOf(impotToken, StringComparison.CurrentCultureIgnoreCase) >= 0) {
							var updateEnd = DateTime.Parse(line.Substring(0, 24));
							stats.Add(new UpdateStat {
								Type = type,
								UpdateDate = begin,
								UserId = Convert.ToUInt32(log["UserId"]),
								Version = System.Version.Parse(log["Version"].ToString()),
								Size = size,
								Time = updateEnd - updateBegin,
							});
							beginFound = false;
							continue;
						}
					}
				}
			}

			foreach (var ver in stats.GroupBy(g => new {g.Version, g.Type}).OrderBy(g => g.Key.Version)) {
				Console.WriteLine("{0} - {1}", ver.Count(), ver.Key);
				Console.WriteLine(ver.Average(x => x.Size).Bytes().ToString("#.##"));
				Console.WriteLine(TimeSpan.FromMilliseconds(ver.Average(x => x.Time.TotalMilliseconds)));
			}
		}

		public void Execute(string user, string password, DateTime begin, string version = null)
		{
			var errors = new List<Error>();
			using(var connection = new MySqlConnection(String.Format("server=sql.analit.net; user={0}; Password={1};", user, password))) {
				connection.Open();
				var s = "";
				if (!string.IsNullOrEmpty(version))
					s = "and version = ?version";
				var sql = String.Format("select * from Logs.ClientAppLogs where CreatedOn > ?begin and userId <> 758 {0} ", s);
				var records = connection.Read(sql, new { begin, version });
				foreach (var record in records) {
					var log = record["Text"].ToString();
					var reader = new StringReader(log);
					var messageHeader = new Regex(@"^\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}\.\d{3} \[\d+\] \w+ ");
					var lastError = new Error {
						Version = record["Version"].ToString()
					};
					string line;
					while ((line = reader.ReadLine()) != null) {
						if (messageHeader.IsMatch(line)) {
							if (lastError.User != null) {
								lastError = new Error {
									Version = record["Version"].ToString()
								};
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
				if (String.IsNullOrEmpty(version))
					Console.WriteLine("Versions = " + @group.Select(g => g.Version ?? "").Distinct().Implode());
				Console.WriteLine("Users = " + @group.Select(g => g.User).Distinct().Implode());
				Console.WriteLine(@group.Key);
				Console.WriteLine();
			}
		}
	}
}