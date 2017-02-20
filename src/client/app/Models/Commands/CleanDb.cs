using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using NHibernate.Linq;
using Common.Tools;
using NHibernate.Dialect;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Util;

namespace AnalitF.Net.Client.Models.Commands
{
	public class CleanDb : DbCommand
	{
		public override void Execute()
		{
			var ignored = new[] {
				"Orders",
				"OrderLines",
				"SentOrders",
				"SentOrderLines",
				"Settings",
				"WaybillSettings",
				"MarkupConfigs",
				"DirMaps",
				"AwaitedItems",
				"Waybills",
				"WaybillLines",
				"WaybillOrders"
			};

			var repair = Configure(new RepairDb());
			repair.Execute();
			using(var sesssion = Factory.OpenSession()) {
				var settings = sesssion.Query<Settings>().FirstOrDefault();
				if (settings != null) {
					settings.LastUpdate = null;
					settings.ServerLastSync = DateTime.MinValue;
					settings.LastSync = DateTime.MinValue;
				}
				sesssion.Flush();

				var dirs = Config.KnownDirs(settings);
				var tables = Tables().ToArray();

				Reporter.Weight(tables.Length + dirs.Count);
				var defaultCatalog = PropertiesHelper.GetString(NHibernate.Cfg.Environment.DefaultCatalog, Configuration.Properties, null);
				var defaultSchema = PropertiesHelper.GetString(NHibernate.Cfg.Environment.DefaultSchema, Configuration.Properties, null);
				foreach (var table in tables) {
					Token.ThrowIfCancellationRequested();
					var dialect = Dialect.GetDialect(Configuration.Properties);
					var schema = dialect.GetDataBaseSchema((DbConnection)sesssion.Connection);
					var columns = schema.GetColumns("data", table.Schema ?? defaultSchema, table.Name, null);
					foreach (var row in columns.AsEnumerable()) {
						var col = new DevartMySQLColumnMetadata(row);
						var mappingCol = table.ColumnIterator.FirstOrDefault(x => String.Equals(x.Name, col.Name, StringComparison.InvariantCultureIgnoreCase));
						if (mappingCol == null) {
							sesssion.CreateSQLQuery($"alter table {table.Name} drop column {col.Name}").ExecuteUpdate();
						}
					}

					if (ignored.Contains(table.Name, StringComparer.InvariantCultureIgnoreCase))
						continue;
					sesssion.CreateSQLQuery($"TRUNCATE {table.Name}").ExecuteUpdate();
					Reporter.Progress();
				}

				foreach (var dir in dirs) {
					try {
						Token.ThrowIfCancellationRequested();
						FileHelper.DeleteDir(dir.Dst);
						Directory.CreateDirectory(dir.Dst);
						Reporter.Progress();
					}
					catch(Exception) {
					}
				}
			}
		}
	}
}