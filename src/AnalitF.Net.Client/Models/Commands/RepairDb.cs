using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Tools;
using NHibernate;

namespace AnalitF.Net.Client.Models.Commands
{
	public class RepairDb : DbCommand<bool>
	{
		public enum RepairStatus
		{
			Ok,
			NumberOfRowsChanged,
			NotExist,
			Fail
		}

		public override void Execute()
		{
			var tables = Tables(Configuration);
			var results = new List<RepairStatus>();

			using(var session = Factory.OpenSession()) {
				foreach (var table in tables) {
					Token.ThrowIfCancellationRequested();

					var messages = ExecuteMaintainsQuery(String.Format("REPAIR TABLE {0} EXTENDED", table), session);
					var result = Parse(messages);
					results.Add(result);
					if (result == RepairStatus.NumberOfRowsChanged || result == RepairStatus.Ok) {
						result = Parse(ExecuteMaintainsQuery(String.Format("OPTIMIZE TABLE {0}", table), session));
						results.Add(result);
					}
					results.Add(result);
					if (result == RepairStatus.Fail) {
						log.ErrorFormat("Таблица {0} не может быть восстановлена.", table);
						log.Error(String.Format("DROP TABLE IF EXISTS {0}", table));
						session
							.CreateSQLQuery(String.Format("DROP TABLE IF EXISTS {0}", table))
							.ExecuteUpdate();
						//если заголовок таблицы поврежден то drop table не даст результатов
						//файлы останутся а при попытке создать таблицу будет ошибка
						//нужно удалить файлы
						Directory.GetFiles(DataPath, table + ".*").Each(File.Delete);
					}
				}
			}

			new SanityCheck(DataPath).Check(true);

			Result = results.All(r => r == RepairStatus.Ok);
		}

		private List<string[]> ExecuteMaintainsQuery(string sql, ISession session)
		{
			log.ErrorFormat(sql);
			var messages = session
				.CreateSQLQuery(sql)
				.List<object[]>()
				.Select(m => m.Select(v => v.ToString()).ToArray())
				.ToList();
			log.Error(messages.Implode(s => s.Implode(), Environment.NewLine));
			return messages;
		}

		private static RepairStatus Parse(IList<string[]> messages)
		{
			//не ведомо что случилось, но нужно верить в лучшее
			if (!messages.Any())
				return RepairStatus.Ok;

			var notExist = messages.Where(m => m[2].Match("error"))
				.Any(m => Regex.Match(m[3], "Table .+ doesn't exist").Success);
			if (notExist)
				return RepairStatus.NotExist;

			var ok = messages.Where(m => m[2].Match("status")).Any(m => m[3].Match("OK"));
			if (ok) {
				var rowsChanged = messages.Where(m => m[2].Match("error"))
					.Any(m => m[3].StartsWith("Number of rows changed"));
				if (rowsChanged)
					return RepairStatus.NumberOfRowsChanged;
				return RepairStatus.Ok;
			}
			return RepairStatus.Fail;
		}
	}
}