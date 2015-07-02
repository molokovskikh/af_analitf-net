using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Tools;
using Common.Tools.Helpers;
using Devart.Data.MySql;
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

		/// <summary>
		/// конструктор предназначин для ситуации когда ты используешь готовую инфраструктуру инициализации
		/// ShellViewModel.RunCmd или Configure.BaseCommand
		/// </summary>
		public RepairDb()
		{
		}

		/// <summary>
		/// конструктор предназначен для ситуации когда вызов производится из контекста где нет инфраструктуры инициализации
		/// </summary>
		public RepairDb(Config.Config config)
		{
			Config = config;
		}

		public override void Execute()
		{
			EnsureInit();
			var tables = TableNames().ToArray();
			Reporter.Weight(tables.Length);
			var results = new List<RepairStatus>();

			foreach (var table in tables) {
				Token.ThrowIfCancellationRequested();

				var messages = ExecuteMaintainsQuery(String.Format("REPAIR TABLE {0} EXTENDED", table));
				var result = Parse(messages);
				results.Add(result);
				if (result == RepairStatus.NumberOfRowsChanged || result == RepairStatus.Ok) {
					result = Parse(ExecuteMaintainsQuery(String.Format("OPTIMIZE TABLE {0}", table)));
					results.Add(result);
				}
				results.Add(result);
				if (result == RepairStatus.Fail) {
					Log.ErrorFormat("Таблица {0} не может быть восстановлена.", table);
					Log.Error(String.Format("DROP TABLE IF EXISTS {0}", table));
					StatelessSession
						.CreateSQLQuery(String.Format("DROP TABLE IF EXISTS {0}", table))
						.ExecuteUpdate();
					//если заголовок таблицы поврежден то drop table не даст результатов
					//файлы останутся а при попытке создать таблицу будет ошибка
					//нужно удалить файлы
					Directory.GetFiles(Config.DbDir, table + ".*").Each(File.Delete);
				}

				Reporter.Progress();
			}

			Configure(new SanityCheck()).Check(true);

			Result = results.All(r => r == RepairStatus.Ok);
		}

		private List<string[]> ExecuteMaintainsQuery(string sql)
		{
			Log.ErrorFormat(sql);
			var messages = StatelessSession
				.CreateSQLQuery(sql)
				.List<object[]>()
				.Select(m => m.Select(v => v.ToString()).ToArray())
				.ToList();
			Log.Error(messages.Implode(s => s.Implode(), Environment.NewLine));
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

		public static bool TryToRepair(Exception exception, Config.Config config)
		{
			var codes = new[] {
				//Incorrect information in file: '%s'
				1033,
				//Incorrect key file for table '%s'; try to repair it
				1034,
				//Old key file for table '%s'; repair it!
				1035,
				//Table '%s' is marked as crashed and should be repaired
				1194,
				//Table '%s' is marked as crashed and last (automatic?) repair failed
				1195,
				//Table upgrade required. Please do "REPAIR TABLE `%s`" to fix it!
				1459
			};
			if (exception.Chain().OfType<MySqlException>().Any(x => codes.Contains(x.Code))) {
				using (var cmd = new RepairDb(config)) {
					cmd.Execute();
					//todo - по хорошему нужно проверить статус и если нашли проблемы, нужно известить человека о том что
					//случилась беда данные были потеряны и отправить на получение каммулятивное обновление
					return true;
				}
			}
			return false;
		}
	}
}