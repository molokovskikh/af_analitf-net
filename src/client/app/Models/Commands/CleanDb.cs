using System;
using System.IO;
using System.Linq;
using NHibernate.Linq;

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
				"AwaitedItems"
			};

			using(var sesssion = Factory.OpenSession()) {
				var settings = sesssion.Query<Settings>().FirstOrDefault();
				if (settings != null)
					settings.LastUpdate = null;
				sesssion.Flush();

				var tables = TableNames().Except(ignored, StringComparer.InvariantCultureIgnoreCase).ToArray();
				var dirs = Config.KnownDirs(settings);

				Reporter.Weight(tables.Length + dirs.Count);
				foreach (var table in tables) {
					Token.ThrowIfCancellationRequested();
					sesssion.CreateSQLQuery(String.Format("TRUNCATE {0}", table))
						.ExecuteUpdate();
					Reporter.Progress();
				}

				foreach (var dir in dirs) {
					try {
						Token.ThrowIfCancellationRequested();
						Directory.Delete(dir.Dst, true);
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