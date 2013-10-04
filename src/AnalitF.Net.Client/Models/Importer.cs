using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;

namespace AnalitF.Net.Client.Models
{
	public class Importer
	{
		private ISession session;
		private Config.Config config;

		private string[] notTruncable = {
			"Waybills",
			"WaybillLines"
		};

		public Importer(ISession session, Config.Config config)
		{
			this.session = session;
			this.config = config;
		}

		public void Import(List<System.Tuple<string, string[]>> tables, ProgressReporter reporter)
		{
			foreach (var table in tables) {
				try {
					var tableName = Path.GetFileNameWithoutExtension(table.Item1);
					var sql = "";

					if (!notTruncable.Contains(tableName, StringComparer.CurrentCultureIgnoreCase))
						sql += String.Format("TRUNCATE {0}; ", tableName);

					sql += String.Format("LOAD DATA INFILE '{0}' REPLACE INTO TABLE {1} ({2})",
						table.Item1.Replace("\\", "/"),
						tableName,
						table.Item2.Implode());

					var dbCommand = session.Connection.CreateCommand();
					dbCommand.CommandText = sql;
					dbCommand.ExecuteNonQuery();
					reporter.Progress(1);
				}
				catch (Exception e) {
					throw new Exception(String.Format("Не могу импортировать {0}", table), e);
				}
			}

			new SanityCheck(config.DbDir).Check();

			var settings = session.Query<Settings>().First();
			var newWaybills = session.Query<Waybill>().Where(w => w.Sum == 0).ToList();
			foreach (var waybill in newWaybills)
				waybill.Calculate(settings);

			settings.LastUpdate = DateTime.Now;
			settings.ApplyChanges(session);
		}
	}
}
