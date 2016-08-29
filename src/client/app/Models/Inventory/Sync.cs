using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models.Commands;
using Common.Tools;
using Dapper;
using Diadoc.Api.Proto;
using Ionic.Zip;
using log4net;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class Sync : RemoteCommand
	{
		public static ILog GlobalLog = LogManager.GetLogger(typeof(Sync));

		protected override UpdateResult Execute()
		{
			var newLastSync = DateTime.Now;
			var lastSync = Settings.LastSync;
			var notSyncedCount = Session.Query<Check>().Count(x => x.Timestamp > lastSync);
			if (notSyncedCount == 0)
				return UpdateResult.OK;

			using (var cleaner = new FileCleaner())
			using (var zipStream = File.Open(cleaner.TmpFile(), FileMode.Open))
			using (var checkStream = File.Open(cleaner.TmpFile(), FileMode.Open))
			using (var lineStream = File.Open(cleaner.TmpFile(), FileMode.Open)) {
				using (var reader = Session.Connection.ExecuteReader("select * from Checks where Timestamp > @lastSync", new { lastSync }))
					WriteTable(reader, checkStream);
				checkStream.Position = 0;

				var sql = @"
select l.*
from CheckLines l
	join Checks c on c.Id = l.CheckId
where c.Timestamp > @lastSync";
				using (var reader = Session.Connection.ExecuteReader(sql, new { lastSync }))
					WriteTable(reader, lineStream);
				lineStream.Position = 0;

				using (var zip = new ZipFile()) {
					zip.AddEntry("check-lines", lineStream);
					zip.AddEntry("checks", checkStream);
					zip.Save(zipStream);
				}
				zipStream.Position = 0;

				var result = Client.PostAsync("Stocks", new StreamContent(zipStream), Token);
				CheckResult(result);
				var import = Configure(new ImportCommand(null));
				import.ImportTables();
				Settings.LastSync = newLastSync;
				using (var trx = Session.BeginTransaction()) {
					Session.Flush();
					trx.Commit();
				}
			}
			return UpdateResult.OK;
		}

		private static void WriteTable(IDataReader reader, FileStream stream)
		{
			var table = new DataTable();
			table.Load(reader);
			table.WriteXml(stream, XmlWriteMode.WriteSchema);
		}

		public static async Task Start(Config.Config config, CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				await TaskEx.Delay(TimeSpan.FromSeconds(10), token);
				if (token.IsCancellationRequested)
					return;

				try {
					using (var sync = new Sync()) {
						sync.InitSession();
						var settings = sync.Session.Query<Settings>().FirstOrDefault();
						if (settings?.IsValid == true) {
							sync.Configure(settings, config, token);
							sync.Execute();
						}
					}
				} catch(Exception e) {
					GlobalLog.Error("Синхронизация завершилась ошибкой", e);
				}
			}
		}
	}
}