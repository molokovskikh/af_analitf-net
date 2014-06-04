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
				"SentOrders",
				"SentOrderLines",
				"Settings",
				"WaybillSettings",
				"MarkupConfigs",
				"DirMaps"
			};
			var tables = TableNames().Except(ignored, StringComparer.InvariantCultureIgnoreCase).ToArray();

			using(var sesssion = Factory.OpenSession()) {
				var settings = sesssion.Query<Settings>().FirstOrDefault();
				if (settings != null)
					settings.LastUpdate = null;
				sesssion.Flush();

				Reporter.Weight(tables.Length);
				foreach (var table in tables) {
					Token.ThrowIfCancellationRequested();
					sesssion.CreateSQLQuery(String.Format("TRUNCATE {0}", table))
						.ExecuteUpdate();
					Reporter.Progress();
				}
			}

			var dirs = new[] {
				"promotions",
				"newses",
				"certificates",
				"attachments",
				"ads"
			};
			foreach (var dir in dirs.Select(d => Path.Combine(Config.RootDir, d))) {
				try {
					Directory.Delete(dir, true);
					Directory.CreateDirectory(dir);
				}
				catch(Exception) {
				}
			}
		}
	}
}