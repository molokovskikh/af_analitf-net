using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate.Linq;
using Common.Tools;

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
					sesssion.CreateSQLQuery($"TRUNCATE {table}")
						.ExecuteUpdate();
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