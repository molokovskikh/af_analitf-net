﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common.Tools;
using Dapper;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.Models.Commands
{
	public class ImportCommand : DbCommand
	{
		private List<Tuple<string, string[]>> data;
		//прайс листы должны экспортироваться перед предложениями для этого им назначается вес -1
		//если мы импортируем предложения и мы получили не полный набор данных то мы должны очистить старые данные
		//прежде чем вставлять новые
		private Dictionary<string, int> weight = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) {
			{ "prices", -1 }
		};

		private static Dictionary<string, string[]> ignoredColumns
			= new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
				{"WaybillLines", new [] {
					//локальные поля
					"Print", "Edited",
					//сертификаты
					"IsError", "IsDownloaded", "IsCertificateNotFound",
					//вычисляемые поля, поиск забраковки
					"IsRejectCanceled", "IsRejectNew", "RejectId", "IsRetailCostFixed",
					//вычисления розничной цены
					"MaxRetailMarkup", "RetailMarkup", "RealRetailMarkup", "RetailCost", "ReceivedQuantity"
				}},
				{"Mails",new [] {
					//локальные поля
					"IsNew", "IsImportant"
				}},
				{"Waybills", new [] {
					//вычисления розничной цены
					"Sum", "RetailSum", "TaxSum", "UserSupplierName", "IsCreatedByUser", "IsRejectChanged", "IsNew", "Error",
					"IsMigrated", "Rounding", "Status", "UserSum", "UserTaxSum"
				}},
				{"Orders", new [] {
					//локальные поля
					"Id", "LinesCount", "Sum", "Send", "Frozen", "PersonalComment", "SendResult", "SendError",
					//может передаваться а может и нет
					"SkipRestore", "IsLoaded"
				}},
				{"OrderLines", new [] {
					//локальные поля
					"IsEditByUser", "RetailMarkup", "RetailCost"
				}},
				{"SentOrderLines", new [] {
					//локальные поля
					"Id", "Comment", "OrderId", "Exp", "Properties", "BuyingMatrixType", "IsEditByUser"
				}},
				{"SentOrders", new [] {
					//локальные поля
					"Id", "LinesCount", "Sum", "PersonalComment", "ReceivingOrderId"
				}},
				{"CheckLines", new [] {
					//локальные поля
					"Id", "CheckId",
				}},
				{"Checks", new [] {
					//локальные поля
					"Id"
				}},
				{"DisplacementDocs", new [] {
					//локальные поля
					"Id",
				}},
				{"DisplacementLines", new [] {
					//локальные поля
					"Id", "DisplacementDocId"
				}},
				{"InventoryDocs", new [] {
					//локальные поля
					"Id"
				}},
				{"InventoryLines", new [] {
					//локальные поля
					"Id", "InventoryDocId"
				}},
				{"ReassessmentDocs", new [] {
					//локальные поля
					"Id"
				}},
				{"ReassessmentLines", new [] {
					//локальные поля
					"Id", "ReassessmentDocId"
				}},
				{"ReturnDocs", new [] {
					//локальные поля
					"Id"
				}},
				{"ReturnLines", new [] {
					//локальные поля
					"Id", "ReturnDocId"
				}},
				{"UnpackingDocs", new [] {
					//локальные поля
					"Id"
				}},
				{"UnpackingLines", new [] {
					//локальные поля
					"Id", "UnpackingDocId"
				}},
				{"WriteoffDocs", new [] {
					//локальные поля
					"Id"
				}},
				{"WriteoffLines", new [] {
					//локальные поля
					"Id", "WriteoffDocId"
				}},
			};

		public bool Strict = true;

		public ImportCommand(string dir)
		{
			data = GetDbData(dir);
		}

		public ImportCommand(List<Tuple<string, string[]>> data)
		{
			this.data = data;
		}

		public override void Execute()
		{
			//перед импортом нужно очистить сессию, тк в процессе импорта могут быть удалены данные которые содержатся в сессии
			//например прайс-листы если на каком то этапе эти данные изменятся и сессия попытается сохранить изменения
			//это приведет к ошибке
			var adresesBeforeImport = Session.Query<Address>().OrderBy(a => a.Name).ToList();
			Session.Clear();
			Reporter.Stage("Импорт данных");
			Reporter.Weight(data.Count);
			ITransaction trx = null;
			if (!Session.Transaction.IsActive)
				trx = Session.BeginTransaction();

			try {
			Session.CreateSQLQuery(@"
drop temporary table if exists UpdatedWaybills;
create temporary table UpdatedWaybills (
	DownloadId int unsigned not null,
	primary key(DownloadId));")
				.ExecuteUpdate();
			ImportTables(new [] { "UpdatedWaybills" });
			ChangeAdress(adresesBeforeImport);
			Session.CreateSQLQuery(@"
update Waybills w
join UpdatedWaybills u on u.DownloadId = w.Id
set w.Status = 1;
drop table if exists UpdatedWaybills;")
				.ExecuteUpdate();
				trx?.Commit();
				trx = null;
			} finally {
				trx?.Rollback();
			}

			Session.Connection.Execute(@"
update CheckLines l
	join Checks d on l.ServerDocId = d.ServerId
set l.CheckId = d.Id
where l.CheckId is null;

update DisplacementLines l
	join DisplacementDocs d on l.ServerDocId = d.ServerId
set l.DisplacementDocId = d.Id
where l.DisplacementDocId is null;

update InventoryLines l
	join InventoryDocs d on l.ServerDocId = d.ServerId
set l.InventoryDocId = d.Id
where l.InventoryDocId is null;

update ReassessmentLines l
	join ReassessmentDocs d on l.ServerDocId = d.ServerId
set l.ReassessmentDocId = d.Id
where l.ReassessmentDocId is null;

update ReturnLines l
	join ReturnDocs d on l.ServerDocId = d.ServerId
set l.ReturnDocId = d.Id
where l.ReturnDocId is null;

update UnpackingLines l
	join UnpackingDocs d on l.ServerDocId = d.ServerId
set l.UnpackingDocId = d.Id
where l.UnpackingDocId is null;

update WriteoffLines l
	join WriteoffDocs d on l.ServerDocId = d.ServerId
set l.WriteoffDocId = d.Id
where l.WriteoffDocId is null;");

			//очистка результатов автозаказа
			//после обновления набор адресов доставки может измениться нужно удаться те позиции которые не будут отображаться
			//если этого не сделать то при повторении дефектуры эти позиции будут загружен под текущим адресом
			Session.CreateSQLQuery(@"delete b
from BatchLines b
left join Addresses a on a.Id = b.AddressId
where a.Id is null;

delete m
from MarkupConfigs m
left join Addresses a on a.Id = m.AddressId
where a.Id is null;

delete s
from AddressConfigs s
left join Addresses a on a.Id = s.AddressId
where a.Id is null;

delete s
from WaybillSettings s
left join Addresses a on a.Id = s.BelongsToAddressId
where a.Id is null;

-- очищаем ожидаемые позиции если товар был удален
delete i
from AwaitedItems i
left join Catalogs c on c.Id = i.CatalogId
where c.Id is null;

delete s
from SpecialMarkupCatalogs s
left join Catalogs c on c.Id = s.CatalogId
where c.Id is null;")
				.ExecuteUpdate();

			Configure(new SanityCheck()).Check();
			var settings = Session.Query<Settings>().First();
			if (IsImported<SentOrder>()) {
				Log.Info("Пересчет отправленных заявок");
				Session.CreateSQLQuery(@"
update SentOrderLines l
	join SentOrders o on l.ServerOrderId = o.ServerId
set l.OrderId = o.Id
where l.OrderId is null;

update SentOrders o
join (
		select sum(l.Count * l.Cost) as sm,
			count(*) as cn, l.OrderId
		from SentOrderLines l
		group by l.OrderId
	) t on t.OrderId = o.Id
set o.Sum = t.sm, o.LinesCount = t.cn
where o.Sum = 0;")
					.ExecuteUpdate();
			}

			//при каждом импорте мы пересчитываем 100 перенесенных накладных что бы избежать
			//ситуации когда после обновления версии нам нужно вычислить десятки тысяч накладных
			//пересчет должен производиться только после импорта данных
			//иначе nhibernate попробует выбрать поставщика и получить null тк база не будет заполнена
			//при сохранении накладной он запишет Null в поле supplierid
			Log.Info("Пересчет перенесенных накладных");
			var products = SpecialMarkupCatalog.Load(StatelessSession.Connection);
			ProcessBatch(
				Session.Query<Waybill>().Where(w => w.Sum == 0)
					.OrderByDescending(x => x.WriteTime).Take(100).Select(x => x.Id).ToArray(),
				(s, x) => {
					foreach (var id in x)
						s.Load<Waybill>(id).Calculate(settings, products);
				});

			if (Session.Query<LoadedDocument>().Any()) {
				Log.Info("Пересчет накладных");
				Session.CreateSQLQuery(@"
update Waybills w
	join LoadedDocuments d on d.Id = w.Id
set IsNew = 1;")
					.ExecuteUpdate();
			}
			if (IsImported<Offer>()) {
				Log.Info("Очистка каталога");
				Session.CreateSQLQuery(@"
drop temporary table if exists ExistsCatalogs;
create temporary table ExistsCatalogs (
	CatalogId int unsigned not null,
	primary key(CatalogId)
);

insert into ExistsCatalogs
select CatalogId from Offers
group by CatalogId;

update Catalogs set HaveOffers = 0;
update CatalogNames set HaveOffers = 0;
update Mnns set HaveOffers = 0;

update ExistsCatalogs e
join Catalogs c on c.Id = e.CatalogId
	join CatalogNames cn on cn.Id = c.NameId
		left join Mnns m on m.Id = cn.MnnId
set m.HaveOffers = 1,
	cn.HaveOffers = 1,
	c.HaveOffers = 1;
drop temporary table ExistsCatalogs;")
					.ExecuteUpdate();

				Log.Info("Пересчет уценки");
				DbMaintain.CalcJunk(StatelessSession, settings);
			}

			//вычисляю таблицы в которых нужно производить чистку
			//Hidden = 1 экспортируется в том случае если позиция была удалена
			//или скрыта и не должна больше быть доступна клиенту
			var cleanupTables = Tables().Where(t => t.ColumnIterator.Any(c => c.Name.Match("Hidden")));
			foreach (var cleanupTable in cleanupTables) {
				var imported = data.Select(d => Path.GetFileNameWithoutExtension(d.Item1))
					.Contains(cleanupTable.Name, StringComparer.CurrentCultureIgnoreCase);
				if (imported) {
					Session.CreateSQLQuery($"delete from {cleanupTable.Name} where Hidden = 1")
						.ExecuteUpdate();
				}
			}

			settings.LastUpdate = DateTime.Now;
			//очищаем кеш изображения что бы перезагрузить его
			Config.Cache.Clear();
			settings.ApplyChanges(Session);
		}

		public void ImportTables(string[] tmpTables = null)
		{
			Log.Info("Начинаю импорт");
			var ordered =
				data.OrderBy(d => Tuple.Create(weight.GetValueOrDefault(Path.GetFileNameWithoutExtension(d.Item1)), d.Item1));
			foreach (var table in ordered) {
				try {
					string sql;
					var tableName = Path.GetFileNameWithoutExtension(table.Item1);
					if (tmpTables?.Contains(tableName, StringComparer.InvariantCultureIgnoreCase) == true) {
						sql = String.Format("LOAD DATA INFILE '{0}' REPLACE INTO TABLE {1}",
							Path.GetFullPath(table.Item1).Replace("\\", "/"),
							tableName);
					} else {
						sql = BuildSql(table);
					}
					if (String.IsNullOrEmpty(sql))
						continue;

					Session.Connection.Execute(sql);
					CheckWarning();
					Reporter.Progress();
				} catch (Exception e) {
					throw new Exception($"Не могу импортировать {table.Item1}", e);
				}
			}
			Log.Info($"Импорт завершен, импортировано {data.Count} таблиц");
		}

		private void ChangeAdress(List<Address> ListAdresesBeforeImport)
		{
			var ListAdresesAfterImport = Session.Query<Address>().OrderBy(a => a.Name).ToList();

			var result = ListAdresesAfterImport.Where(n => ListAdresesBeforeImport.Any(t => t.Id == n.Id && t.Name != n.Name));
			foreach (var adr in result)
			{
				WaybillSettings waybillSettings = Session.Query<WaybillSettings>().FirstOrDefault(x => x.BelongsToAddress.Id == adr.Id);
				if (ListAdresesBeforeImport.Where(n => n.Id == waybillSettings.BelongsToAddress.Id && n.Name == waybillSettings.Address).Count() > 0)
				{
					waybillSettings.Address = adr.Name;
					Session.Save(waybillSettings);
				}
			}
		}

		private bool IsImported<T>()
		{
			return data
				.Select(d => Path.GetFileNameWithoutExtension(d.Item1))
				.Contains(Inflector.Inflector.Pluralize(typeof(T).Name), StringComparer.CurrentCultureIgnoreCase);
		}

		private string BuildSql(Tuple<string, string[]> table)
		{
			var tableName = Path.GetFileNameWithoutExtension(table.Item1);
			if (tableName.Match("cmds")) {
				return File.ReadAllText(table.Item1);
			}
			//пока сервис не имеет ни какой обратной совместимости
			//клиент должен сам разобраться что он может обработать а что нет
			var dbTable = Tables().FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
			if (dbTable == null) {
#if DEBUG
				throw new Exception($"Импорт неизвестной таблицы {tableName}");
#else

				return null;
#endif
			}

			var sql = "";
			var exportedColumns = table.Item2;
			if (exportedColumns.FirstOrDefault().Match("truncate")) {
				exportedColumns = exportedColumns.Skip(1).ToArray();
				sql += $"TRUNCATE {tableName}; ";
			}
			else if (tableName.Match("offers")) {
				//мы должны удалить все предложения по загруженным прайс-листам и предложения по отсутствующим прайс-листам
				sql += @"
delete o from Offers o
	left join Prices p on p.PriceId = o.PriceId and p.RegionId = o.RegionId
where p.IsSynced = 1 or p.PriceId is null;";
			}

			//некоторые колонки могут отсутствовать в базе но быть в данных
			//имена таких колонок заменяем на @<название> что вместо таблицы они попадали
			//в переменную
			var tableColumns = dbTable.ColumnIterator.Select(cl => cl.Name).ToArray();
			var targetColumns = exportedColumns.Select(c => {
				if (tableColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
					return c;
				else
					return "@" + c;
			}).Implode();
			StrictCheck(dbTable, tableColumns, exportedColumns);
			sql += String.Format("LOAD DATA INFILE '{0}' REPLACE INTO TABLE {1} ({2})",
				Path.GetFullPath(table.Item1).Replace("\\", "/"),
				tableName,
				targetColumns);
			return sql;
		}

		[Conditional("DEBUG")]
		private void CheckWarning()
		{
			if (!Strict)
				return;
			var warnings = Session.Connection.Query("show warnings").Select(x => (string)x.Message).ToList();
			if (warnings.Count > 0)
				throw new Exception(warnings.Implode());
		}

		[Conditional("DEBUG")]
		private void StrictCheck(Table dbTable, string[] tableColumns, string[] exportedColumns)
		{
			if (!Strict)
				return;
			if (dbTable.Name.Match("stocks"))
				return;
			//код для отладки, при тестировании мы должны передавать\принимать все колонки таблицы
			//проверяем что все колонки которые есть в таблице передаются с сервера
			var ignored = new[] {
				"Timestamp", //нужен для синхронизации не должен импортироваться
				"HaveOffers", //всегда вычисляется на стороне клиента
				"Hidden", //не передается в случае кумулятивного обновления
				//информация о лидерах вычисляется на клиенте тк не может быть достоверно вычислена на сервере
				"LeaderPriceId",
				"LeaderRegionId",
				"LeaderCost",
				"MinBoundCost",
				"MaxBoundCost",
				"Marked",
			};
			if (dbTable.Name.Match("DelayOfPayments") || dbTable.Name.Match("BatchLines")) {
				ignored = ignored.Concat(new[] { "Id" }).ToArray();
			}
			if (dbTable.Name.Match("OrderLines")) {
				ignored = ignored.Concat(new[] {
					"Id", "SendError", "SendResult", "OrderId",
					"NewCost", "OldCost", "OptimalFactor", "NewQuantity", "OldQuantity", "Comment",
					"BuyingMatrixType", //нет смысла передавать статус матрицы
					"Properties", //todo не вычисляются исправить
					"Exp", //todo не сохраняются исправить
					"ExportId", "ExportBatchLineId"
				})
					.ToArray();
			}
			if (dbTable.Name.Match("Attachments")) {
				ignored = ignored.Concat(new[] {
					//локальные поля
					"LocalFilename", "IsError", "IsDownloaded"
				})
					.ToArray();
			}
			if (dbTable.Name.Match("Orders")) {
				ignored = ignored.Concat(new[] {
					"DisplayId",
					"KeepId",
					"SavePriceName",
					"SaveRegionName",
					"SavePriceDate"
				})
					.ToArray();
			}

			ignored = ignored.Concat(ignoredColumns.GetValueOrDefault(dbTable.Name, new string[0])).ToArray();
			var columnsToCheck = tableColumns.Except(ignored, StringComparer.OrdinalIgnoreCase).ToArray();
			var notFoundInTable = exportedColumns.Except(tableColumns, StringComparer.OrdinalIgnoreCase).ToArray();
			var notFoundInData = columnsToCheck.Except(exportedColumns, StringComparer.OrdinalIgnoreCase).ToArray();
			if (notFoundInTable.Length > 0) {
				throw new Exception($"В таблице {dbTable.Name} не найдены колонки полученные с сервера {notFoundInTable.Implode()}");
			}
			if (notFoundInData.Length > 0) {
				throw new Exception($"В таблице {dbTable.Name} есть колонки которые отсутствуют в данных {notFoundInData.Implode()}");
			}
		}

		public static List<Tuple<string, string[]>> GetDbData(string dir)
		{
			var files = Directory.GetFiles(dir).Select(Path.GetFileName);
			return files.Where(f => f.EndsWith("meta.txt"))
				.Select(f => Tuple.Create(f, files.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d)
					.Match(f.Replace(".meta.txt", "")))))
				.Where(t => t.Item2 != null)
				.Select(t => Tuple.Create(
					Path.GetFullPath(Path.Combine(dir, t.Item2)),
					File.ReadAllLines(Path.Combine(dir, t.Item1))))
				.Concat(files.Where(x => Path.GetFileNameWithoutExtension(x).Match("cmds"))
					.Select(x => Tuple.Create(Path.Combine(dir, x), new string[0])))
				.ToList();
		}
	}
}
