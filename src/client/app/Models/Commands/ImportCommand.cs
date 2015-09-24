using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.Models.Commands
{
	public class ImportCommand : DbCommand
	{
		private List<System.Tuple<string, string[]>> data;
		//прайс листы должны экспортироваться перед предложениями для этого им назначается вес -1
		//если мы импортируем предложения и мы получили не полный набор данных то мы должны очистить старые данные
		//прежде чем вставлять новые
		private Dictionary<string, int> weight = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) {
			{ "prices", -1 }
		};

#if DEBUG
		private static Dictionary<string, string[]> ignoredColumns
			= new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
				{"WaybillLines", new [] {
					//локальные поля
					"Print", "Edited",
					//сертификаты
					"IsError", "IsDownloaded", "IsCertificateNotFound",
					//вычисляемые поля, поиск забраковки
					"IsRejectCanceled", "IsRejectNew", "RejectId",
					//вычисления розничной цены
					"MaxRetailMarkup", "RetailMarkup", "RealRetailMarkup", "RetailCost",
				}},
				{"Mails",new [] {
					//локальные поля
					"IsNew", "IsImportant"
				}},
				{"Waybills", new [] {
					//вычисления розничной цены
					"Sum", "RetailSum", "TaxSum", "UserSupplierName", "IsCreatedByUser", "IsRejectChanged", "IsNew", "Error"
				}},
				{"Orders", new [] {
					//локальные поля
					"Id", "LinesCount", "Sum", "Send", "Frozen", "PersonalComment", "SendResult", "SendError",
					//может передаваться а может и нет
					"SkipRestore", "IsLoaded"
				}},
				{"SentOrderLines", new [] {
					//локальные поля
					"Id", "Comment", "OrderId", "Exp", "Properties", "BuyingMatrixType"
				}},
				{"SentOrders", new [] {
					//локальные поля
					"Id", "LinesCount", "Sum", "PersonalComment"
				}}
			};

		public bool Strict = true;
#endif

		public ImportCommand(List<System.Tuple<string, string[]>> data)
		{
			this.data = data;
		}

		public override void Execute()
		{
			//перед импортом нужно очистить сессию, тк в процессе импорта могут быть удалены данные которые содержатся в сессии
			//например прайс-листы если на каком то этапе эти данные изменятся и сессия попытается сохранить изменения
			//это приведет к ошибке
			Session.Clear();
			Reporter.Stage("Импорт данных");
			Reporter.Weight(data.Count);
			var ordered = data.OrderBy(d => Tuple.Create(weight.GetValueOrDefault(Path.GetFileNameWithoutExtension(d.Item1)), d.Item1));
			foreach (var table in ordered) {
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
					throw new Exception(String.Format("Не могу импортировать {0}", table.Item1), e);
				}
			}

			Configure(new SanityCheck()).Check();

			var settings = Session.Query<Settings>().First();
			if (IsImported<SentOrder>()) {
				Session.CreateSQLQuery(@"
update SentOrderLines l
	join SentOrders o on l.ServerOrderId = o.ServerId
set l.OrderId = o.Id
where l.OrderId is null;

update SentOrders o
set Sum = (select sum(round(l.Cost * l.Count, 2)) from SentOrderLines l where l.OrderId = o.Id),
	LinesCount = (select count(*) from SentOrderLines l where l.OrderId = o.Id)
where Sum = 0;")
					.ExecuteUpdate();
			}
			if (Session.Query<LoadedDocument>().Any()) {
				Session.CreateSQLQuery(@"
update Waybills set IsNew = 0;
update Waybills w
	join LoadedDocuments d on d.Id = w.Id
set IsNew = 1;")
					.ExecuteUpdate();
				var newWaybills = Session.Query<Waybill>().Where(w => w.Sum == 0).ToList();
				foreach (var waybill in newWaybills)
					waybill.Calculate(settings);
			}
			if (IsImported<Offer>()) {
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
				DbMaintain.UpdateLeaders(Session, settings);
				DbMaintain.CalcJunk(StatelessSession, settings);
			}

			//очистка результатов автозаказа
			//после обновления набор адресов доставки может измениться нужно удаться те позиции которые не будут отображаться
			//если этого не сделать то при повторении дефектуры эти позиции будут загружен под текущим адресом
			Session.CreateSQLQuery(@"delete b
from BatchLines b
left join Addresses a on a.Id = b.AddressId
where a.Id is null;")
				.ExecuteUpdate();
			//очищаем ожидаемые позиции если товар был удален
			Session.CreateSQLQuery(@"delete i
from AwaitedItems i
left join Catalogs c on c.Id = i.CatalogId
where c.Id is null")
				.ExecuteUpdate();

			//вычисляю таблицы в которых нужно производить чистку
			//Hidden = 1 экспортируется в том случае если позиция была удалена
			//или скрыта и не должна больше быть доступна клиенту
			var cleanupTables = Tables().Where(t => t.ColumnIterator.Any(c => c.Name.Match("Hidden")));
			foreach (var cleanupTable in cleanupTables) {
				var imported = data.Select(d => Path.GetFileNameWithoutExtension(d.Item1))
					.Contains(cleanupTable.Name, StringComparer.CurrentCultureIgnoreCase);
				if (imported) {
					Session.CreateSQLQuery(String.Format("delete from {0} where Hidden = 1", cleanupTable.Name))
						.ExecuteUpdate();
				}
			}

			settings.LastUpdate = DateTime.Now;
			//очищаем кеш изображения что бы перезагрузить его
			Settings.ImageCache = null;
			settings.ApplyChanges(Session);
		}

		private bool IsImported<T>()
		{
			return data
				.Select(d => Path.GetFileNameWithoutExtension(d.Item1))
				.Contains(Inflector.Inflector.Pluralize(typeof(T).Name), StringComparer.CurrentCultureIgnoreCase);
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
			var exportedColumns = table.Item2;
			if (exportedColumns.FirstOrDefault().Match("truncate")) {
				exportedColumns = exportedColumns.Skip(1).ToArray();
				sql += String.Format("TRUNCATE {0}; ", tableName);
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
#if DEBUG
			if (Strict) {
				//код для отладки, при тестировании мы должны передавать\принимать все колонки таблицы
				//проверяем что все колонки котоые есть в таблице передаются с сервера
				var ignored = new [] {
					"Timestamp",//нужен для синхронизации не доджен импортироваться
					"HaveOffers",//всегда вычисляется на стророне клиента
					"Hidden",//не передается в случае каммулятивного обновления
					//информация о лидерах вичисляется на клиенте тк не может быть достоверно вычислена на сервере
					"LeaderPriceId",
					"LeaderRegionId",
					"LeaderCost",
					"MinBoundCost",
					"MaxBoundCost",
					"Marked",
				};
				if (dbTable.Name.Match("DelayOfPayments") || dbTable.Name.Match("BatchLines")) {
					ignored = ignored.Concat(new [] { "Id" }).ToArray();
				}
				if (dbTable.Name.Match("OrderLines")) {
					ignored = ignored.Concat(new [] { "Id", "SendError", "SendResult", "OrderId",
						"NewCost", "OldCost", "OptimalFactor", "NewQuantity", "OldQuantity", "Comment",
						"BuyingMatrixType",//нет смысла передавать статус матрицы
						"Properties",//todo не вычисляются исправить
						"Exp",//todo не сохраняются исправить
						"ExportId", "ExportBatchLineId"
					})
					.ToArray();
				}
				if (dbTable.Name.Match("Attachments")) {
					ignored = ignored.Concat(new [] {
						//локальные поля
						"LocalFilename", "IsError", "IsDownloaded"
					})
					.ToArray();
				}

				ignored = ignored.Concat(ignoredColumns.GetValueOrDefault(dbTable.Name, new string[0])).ToArray();
				var columnsToCheck = tableColumns.Except(ignored, StringComparer.OrdinalIgnoreCase).ToArray();
				var notFoundInTable = exportedColumns.Except(tableColumns, StringComparer.OrdinalIgnoreCase).ToArray();
				var notFoundInData = columnsToCheck.Except(exportedColumns, StringComparer.OrdinalIgnoreCase).ToArray();
				if (notFoundInTable.Length > 0) {
					throw new Exception(String.Format("В таблице {0} не найдены колонки полученные с сервера {1}",
						dbTable.Name, notFoundInTable.Implode()));
				}
				if (notFoundInData.Length > 0) {
					throw new Exception(String.Format("В таблице {0} есть колонки которые отсутствуют в данных {1}",
						dbTable.Name, notFoundInData.Implode()));
				}
			}
#endif
			sql += String.Format("LOAD DATA INFILE '{0}' REPLACE INTO TABLE {1} ({2})",
				Path.GetFullPath(table.Item1).Replace("\\", "/"),
				tableName,
				targetColumns);
			return sql;
		}
	}
}
