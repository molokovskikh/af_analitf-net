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
					throw new Exception(String.Format("Не могу импортировать {0}", table), e);
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
			if (IsImported<Waybill>()) {
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
			}

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
			var meta = table.Item2;
			if (meta.FirstOrDefault().Match("truncate")) {
				meta = meta.Skip(1).ToArray();
				sql += String.Format("TRUNCATE {0}; ", tableName);
			}
			else if (tableName.Match("offers")) {
				//мы должны удалить все предложения по загруженным прайс-листам и предложения по отсутствующим прайс-листам
				sql += @"
delete o from Offers o
	left join Prices p on p.PriceId = o.PriceId and p.RegionId = o.RegionId
where p.IsSynced = 1 or p.PriceId is null;";
			}

			var columns = meta;
			//некоторые колонки могут отсутствовать в базе но быть в данных
			//имена таких колонок заменяем на @<название> что вместо таблицы они попадали
			//в переменную
			var targetColumns = columns.Select(c => {
				if (dbTable.ColumnIterator.Select(cl => cl.Name).Contains(c, StringComparer.OrdinalIgnoreCase))
					return c;
				else
					return "@" + c;
			}).Implode();
			sql += String.Format("LOAD DATA INFILE '{0}' REPLACE INTO TABLE {1} ({2})",
				Path.GetFullPath(table.Item1).Replace("\\", "/"),
				tableName,
				targetColumns);
			return sql;
		}
	}
}
