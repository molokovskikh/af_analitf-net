using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Tools;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Commands
{
	public class ImportCommand : DbCommand
	{
		private List<System.Tuple<string, string[]>> data;

		public ImportCommand(List<System.Tuple<string, string[]>> data)
		{
			this.data = data;
		}

		public override void Execute()
		{
			//перед импортом нужно очистить сессию, тк в процессе импорта могут быть удалены данные которые содержатся в сесии
			//например прайс-листы если на каком то этапе эти данные изменятся и сессия попытается сохранить изменения
			//это приведет к ошибке
			Session.Clear();
			Reporter.Stage("Импорт данных");
			Reporter.Weight(data.Count);
			foreach (var table in data) {
				try {
					var sql = BuildSql(table);
					if (String.IsNullOrEmpty(sql))
						continue;

					var dbCommand = Session.Connection.CreateCommand();
					dbCommand.CommandText = sql;
					dbCommand.ExecuteNonQuery();
					Reporter.Progress();
				}
				catch (Exception e) {
					throw new Exception(String.Format("Не могу импортировать {0}", table), e);
				}
			}

			Configure(new SanityCheck()).Check();

			var settings = Session.Query<Settings>().First();
			var newWaybills = Session.Query<Waybill>().Where(w => w.Sum == 0).ToList();
			foreach (var waybill in newWaybills)
				waybill.Calculate(settings);

			settings.LastUpdate = DateTime.Now;
			settings.ApplyChanges(Session);
		}

		private string BuildSql(System.Tuple<string, string[]> table)
		{
			var tableName = Path.GetFileNameWithoutExtension(table.Item1);
			if (tableName.Match("cmds")) {
				return File.ReadAllText(table.Item1);
			}
			//пока сервис не имеет ни какой обратной совместимости
			//клиент должен сам разобраться что он может обработать а что нет
			var dbTable = Tables().FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
			if (dbTable == null)
				return null;

			var sql = "";
			var meta = table.Item2;
			if (meta.FirstOrDefault().Match("truncate")) {
				meta = meta.Skip(1).ToArray();
				sql += String.Format("TRUNCATE {0}; ", tableName);
			}

			var columns = meta;
			//некоторые колонки могут отсутствовать в базе но быть в данных
			//имена таких колонок заменяем на @<название> что вместо таблицы они попадали
			//в переменную
			var targetColumns = columns.Select(c => {
				if (dbTable.ColumnIterator.Select(cl => cl.Name).Contains(c, StringComparer.OrdinalIgnoreCase))
					return c;
				else
					return "@" + c;
			}).Implode();
			sql += String.Format("LOAD DATA INFILE '{0}' REPLACE INTO TABLE {1} ({2})",
				Path.GetFullPath(table.Item1).Replace("\\", "/"),
				tableName,
				targetColumns);
			return sql;
		}
	}
}
