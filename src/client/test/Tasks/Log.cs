using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Common.MySql;
using Common.Tools;
using Humanizer;
using MySql.Data.MySqlClient;

namespace AnalitF.Net.Client.Test.Tasks
{
	public class Log
	{
		//некоторые ошибки не интересны
		private string[] networkErrors = {
			"System.Net.WebException: Невозможно разрешить удаленное имя",
			"System.Net.WebException: The remote name could not be resolved",
			"System.Net.WebException: The operation has timed out.",
			"System.Net.Sockets.SocketException: Попытка установить соединение была безуспешной",
			"System.Net.Sockets.SocketException: Сделана попытка выполнить операцию на сокете при отключенной сети",
			"System.Net.Sockets.SocketException: Сделана попытка доступа к сокету методом, запрещенным правами доступа",
			"System.Net.Sockets.SocketException: Программа на вашем хост-компьютере разорвала установленное подключение",
			"System.Net.Sockets.SocketException: Удаленный хост принудительно разорвал существующее подключение",
			//todo временно до релиза
			"AnalitF.Net.Client.Models.Commands.RepairDb",
			//todo нужно подумать как быть с out of memory но пока игнорируем
			"System.OutOfMemoryException: Insufficient memory to continue the execution of the program.",
			"System.OutOfMemoryException: Недостаточно памяти для продолжения выполнения программы.",
			//думаю что это тоже out of memory но из mysql
			"Got error 134 from storage engine",
		};
		private Regex[] cleanup = {
			new Regex(@"\(HashCode=\d+\)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
			new Regex(@"токен = \S+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
		};

		public class Error
		{
			public string User;
			public string Version;
			public DateTime Date;
			public string Text;
			public string StackTrace;
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
								Version = Version.Parse(log["Version"].ToString()),
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

		public void Execute(string user, string password, DateTime begin, string version = null, int minCount = 0)
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
					var messageHeader = new Regex(@"^\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}\.\d{3} \[\d+\] (?<level>\w+) (?<text>.*)");
					Error lastError = null;
					string line;
					while ((line = reader.ReadLine()) != null) {
						var match = messageHeader.Match(line);
						if (match.Success) {
							if (match.Groups["level"].Value == "ERROR") {
								lastError = new Error {
									Version = record["Version"].ToString(),
									User = record["UserId"].ToString(),
									Text = match.Groups["text"].Value + "\r\n"
								};
								errors.Add(lastError);
							}
						}
						else {
							if (!String.IsNullOrEmpty(lastError.StackTrace) || line.StartsWith("   at ") || line.StartsWith("   в "))
								lastError.StackTrace += line + "\r\n";
							else
								lastError.Text += line + "\r\n";
						}
					}
				}
			}

			errors = errors.Where(x => !networkErrors.Any(y => x.Text.Contains(y))).ToList();
			//некоторые ошибки могут быть одинаковыми по сути но содержать переменные данные
			//очищаем сообщения от таких данных
			foreach (var regex in cleanup) {
				errors.Each(x => x.Text = regex.Replace(x.Text, "<var>"));
			}
			var groups = errors.GroupBy(e => e.Text).OrderByDescending(g => g.Count());
			var index = 0;
			var skip = 0;
			foreach (var @group in groups) {
				if (@group.Count() < minCount) {
					skip++;
					continue;
				}
				Console.WriteLine("{0})Count = " + @group.Count(), index);
				if (String.IsNullOrEmpty(version))
					Console.WriteLine("Versions = " + @group.Select(g => g.Version ?? "").Distinct().Implode());
				Console.WriteLine("Users = " + @group.Select(g => g.User).Distinct().Implode());
				Console.WriteLine((@group.Key ?? "").Trim());
				Console.WriteLine((@group.First().StackTrace ?? "").Trim());
				Console.WriteLine();
				index++;
			}

			if (skip > 0)
				Console.WriteLine("skiped {0} errors", skip);
		}
	}
}