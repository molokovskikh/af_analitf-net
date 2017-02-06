using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using Common.Tools;
using Dapper;
using Diadoc.Api.Proto;
using Ionic.Zip;
using log4net;
using Newtonsoft.Json;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class SyncCommand : RemoteCommand
	{
		public static ILog GlobalLog = LogManager.GetLogger(typeof(SyncCommand));

		protected override UpdateResult Execute()
		{
			var newLastSync = DateTime.Now;
			using (var disposable = new CompositeDisposable())
			using (var zipStream = File.Open(Cleaner.TmpFile(), FileMode.Open))
			using (var checkStream = File.Open(Cleaner.TmpFile(), FileMode.Open))
			using (var lineStream = File.Open(Cleaner.TmpFile(), FileMode.Open)) {
				using (var reader = Session.Connection.ExecuteReader("select * from Checks where Timestamp > @lastSync",
						new { lastSync = Settings.LastSync }))
					WriteTable(reader, checkStream);

				var sql = @"
select l.*
from CheckLines l
	join Checks c on c.Id = l.CheckId
where c.Timestamp > @lastSync";
				using (var reader = Session.Connection.ExecuteReader(sql, new {lastSync = Settings.LastSync }))
					WriteTable(reader, lineStream);

				var actions = Session.Connection
					.Query<StockAction>("select * from StockActions where Timestamp > @lastSync",
						new { lastSync = Settings.LastSync })
					.ToArray();

				using (var zip = new ZipFile()) {
					zip.AddEntry("check-lines", lineStream);
					zip.AddEntry("checks", checkStream);
					zip.AddEntry("server-timestamp", Settings.ServerLastSync.ToString("O"));
					zip.AddEntry("stock-actions", JsonConvert.SerializeObject(actions));
					WriteModel(zip, disposable, typeof(DisplacementDoc));
					WriteSql(zip, disposable, "DisplacementLines", @"
select l.*
from DisplacementLines l
	join DisplacementDocs d on d.Id = l.DisplacementDocId
where d.Timestamp > @lastSync");

					WriteModel(zip, disposable, typeof(InventoryDoc));
					WriteSql(zip, disposable, "InventoryLines", @"
select l.*
from InventoryLines l
	join InventoryDocs d on d.Id = l.InventoryDocId
where d.Timestamp > @lastSync");

					WriteModel(zip, disposable, typeof(ReassessmentDoc));
					WriteSql(zip, disposable, "ReassessmentLines", @"
select l.*
from ReassessmentLines l
	join ReassessmentDocs d on d.Id = l.ReassessmentDocId
where d.Timestamp > @lastSync");

					WriteModel(zip, disposable, typeof(ReturnDoc));
					WriteSql(zip, disposable, "ReturnLines", @"
select l.*
from ReturnLines l
	join ReturnDocs d on d.Id = l.ReturnDocId
where d.Timestamp > @lastSync");

					WriteModel(zip, disposable, typeof(WriteoffDoc));
					WriteSql(zip, disposable, "WriteoffLines", @"
select l.*
from WriteoffLines l
	join WriteoffDocs d on d.Id = l.WriteoffDocId
where d.Timestamp > @lastSync");

					WriteModel(zip, disposable, typeof(UnpackingDoc));
					WriteSql(zip, disposable, "UnpackingLines", @"
select l.*
from UnpackingLines l
	join UnpackingDocs d on d.Id = l.UnpackingDocId
where d.Timestamp > @lastSync");

					WriteSql(zip, disposable, "Waybills", @"
select Id, Timestamp
from Waybills
where Timestamp > @lastSync");

					zip.Save(zipStream);
				}
				zipStream.Position = 0;

				var result = Client.PostAsync("Stocks", new StreamContent(zipStream), Token);
				CheckResult(result);
				var dir = Cleaner.RandomDir();
				using (var file = File.Open(Cleaner.TmpFile(), FileMode.Open)) {
					result.Result.Content.CopyToAsync(file).Wait();
					file.Position = 0;
					using (var zip = ZipFile.Read(file))
						zip.ExtractAll(dir, ExtractExistingFileAction.OverwriteSilently);
				}

				var import = Configure(new ImportCommand(dir) {
					Strict = false
				});
				import.ImportTables();
				Settings.LastSync = newLastSync;
				Settings.ServerLastSync = DateTime.Parse(File.ReadAllText(Path.Combine(dir, "server-timestamp")));
				//using (var trx = Session.BeginTransaction()) {
				//	Session.Flush();
				//	trx.Commit();
				//}
			}
			return UpdateResult.OK;
		}

		private void WriteModel(ZipFile zip, CompositeDisposable disposable, Type type)
		{
			var mapping = Configuration.GetClassMapping(type);
			var sql = $"select * from {mapping.Table.Name} where Timestamp > @lastSync";
			WriteSql(zip, disposable, mapping.Table.Name, sql);
		}

		private void WriteSql(ZipFile zip, CompositeDisposable disposable, string name, string sql)
		{
			var stream = File.Open(Cleaner.TmpFile(), FileMode.Open);
			disposable.Add(stream);
			using (var reader = Session.Connection.ExecuteReader(sql, new { lastSync = Settings.LastSync }))
				WriteTable(reader, stream);
			zip.AddEntry(name, stream);
		}

		private static void WriteTable(IDataReader reader, FileStream stream)
		{
			var table = new DataTable();
			table.Load(reader);
			table.WriteXml(stream, XmlWriteMode.WriteSchema);
			stream.Position = 0;
		}

		public static async Task Start(Config.Config config,
			ManualResetEventSlim startEvent,
			CancellationToken token,
			NotifyValue<User> user)
		{
			while (!token.IsCancellationRequested) {
				await TaskEx.WhenAny(TaskEx.Delay(TimeSpan.FromMinutes(10), token), TaskEx.Run(() => startEvent.Wait()));
				if (token.IsCancellationRequested)
					return;
				if (user.Value?.IsStockEnabled == false)
					continue;

				startEvent.Reset();
				try {
					using (var sync = new SyncCommand()) {
						sync.InitSession();
						var settings = sync.Session.Query<Settings>().FirstOrDefault();
						if (settings?.IsValid == true) {
							sync.Configure(settings, config, token);
							sync.Execute();
							Env.Current.Bus.SendMessage("stocks", "reload");
						}
					}
				} catch(Exception e) {
					GlobalLog.Error("Синхронизация завершилась ошибкой", e);
				}
			}
		}
	}
}