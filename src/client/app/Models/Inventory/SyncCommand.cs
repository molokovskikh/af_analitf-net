﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Config;
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
			using (var zipStream = File.Open(Cleaner.TmpFile(), FileMode.Open))
			using (var checkStream = File.Open(Cleaner.TmpFile(), FileMode.Open))
			using (var lineStream = File.Open(Cleaner.TmpFile(), FileMode.Open)) {
				using (var reader = Session.Connection.ExecuteReader("select * from Checks where Timestamp > @lastSync",
						new { lastSync = Settings.LastSync }))
					WriteTable(reader, checkStream);
				checkStream.Position = 0;

				var sql = @"
select l.*
from CheckLines l
	join Checks c on c.Id = l.CheckId
where c.Timestamp > @lastSync";
				using (var reader = Session.Connection.ExecuteReader(sql, new {lastSync = Settings.LastSync }))
					WriteTable(reader, lineStream);
				lineStream.Position = 0;

				var actions = Session.Connection
					.Query<StockAction>("select * from StockActions where Timestamp > @lastSync",
						new { lastSync = Settings.LastSync })
					.ToArray();

				using (var zip = new ZipFile()) {
					zip.AddEntry("check-lines", lineStream);
					zip.AddEntry("checks", checkStream);
					zip.AddEntry("server-timestamp", Settings.ServerLastSync.ToString("O"));
					zip.AddEntry("stock-actions", JsonConvert.SerializeObject(actions));
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

		public static async Task Start(Config.Config config,
			ManualResetEventSlim startEvent,
			CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				await TaskEx.WhenAny(TaskEx.Delay(TimeSpan.FromMinutes(10), token), TaskEx.Run(() => startEvent.Wait()));
				if (token.IsCancellationRequested)
					return;

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