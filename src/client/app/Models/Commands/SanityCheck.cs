using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AnalitF.Net.Client.Config.NHibernate;
using Common.NHibernate;
using Common.Tools;
using Devart.Data.MySql;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Dialect.Schema;
using NHibernate.Engine;
using NHibernate.Exceptions;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Util;

namespace AnalitF.Net.Client.Models.Commands
{
	public class SanityCheck : BaseCommand
	{
		public bool Debug;

		public SanityCheck()
		{
		}

		public SanityCheck(Config.Config config)
		{
			Config = config;
		}

		/// <summary>
		/// true - если схема была обновлена
		/// false - если схема не обновлялась
		/// </summary>
		/// <param name="updateSchema"></param>
		/// <returns></returns>
		public bool Check(bool updateSchema = false)
		{
			if (!Directory.Exists(Config.DbDir)) {
				Directory.CreateDirectory(Config.DbDir);
				InitDb();
			}
			else if (updateSchema) {
				UpgradeSchema();
				UpgradeData();
			}

			bool crushOnFirstTry;
			try {
				crushOnFirstTry = CheckSettings(updateSchema);
			}
			catch(GenericADOException e) {
				//Unknown column '%s' in '%s'
				//Table '%s.%s' doesn't exist
				//http://dev.mysql.com/doc/refman/5.0/en/error-messages-server.html
				if (e.InnerException is MySqlException &&
					(((MySqlException)e.InnerException).Code == 1054
						|| ((MySqlException)e.InnerException).Code == 1146)) {
					crushOnFirstTry = true;
					Log.Error("База данных повреждена попробую восстановить", e);
				}
				else {
					throw;
				}
			}

			//если свалились на первой попытке нужно попытаться починить базу и попробовать еще разик
			if (crushOnFirstTry) {
				UpgradeSchema();
				CheckSettings(true);
				return true;
			}
			return false;
		}

		private bool CheckSettings(bool overrideHash)
		{
			using (var session = Factory.OpenSession())
			using (var transaction = session.BeginTransaction()) {
				var settings = session.Query<Settings>().FirstOrDefault();
				var mappingToken = AppBootstrapper.NHibernate.MappingHash;
				if (settings == null) {
					settings = new Settings(mappingToken, session.Query<Address>().ToArray());
					settings.CheckToken();
					session.Save(settings);
				}
				else {
					settings.CheckToken();
					if (overrideHash)
						settings.MappingToken = mappingToken;

					if (settings.MappingToken != mappingToken)
						return true;

					if (settings.Waybills.Count == 0)
						session.Query<WaybillSettings>().Each(settings.Waybills.Add);
				}

				var addresses = session.Query<Address>().ToList();
				var mainAddress = addresses.FirstOrDefault();
				if (mainAddress != null) {
					if (settings.Markups.All(x => x.Address == null))
						settings.Markups.Each(x => x.Address = mainAddress);
				}
				foreach (var address in addresses.Except(new [] { mainAddress }))
					settings.CopyMarkups(mainAddress, address);
					//если ничего восстановить не удалось тогда берем значения по умолчанию
				foreach (var address in addresses) {
					if (settings.Markups.Count(x => x.Address == address) == 0)
						MarkupConfig.Defaults(address).Each(settings.AddMarkup);
				}
				if (settings.Markups.Count(x => x.Type == MarkupType.Nds18) == 0) {
					settings.Markups.AddEach(settings.Markups.Where(x => x.Type == MarkupType.Over)
						.Select(x => new MarkupConfig(x, x.Address) {
							Type = MarkupType.Nds18,
						}));
				}

				//если есть адреса то должен быть и пользователь
				//если только база не была поломана
				var user = session.Query<User>().FirstOrDefault()
					?? new User();

				settings.Waybills.AddEach(addresses
					.Except(settings.Waybills.Select(w => w.BelongsToAddress))
					.Select(a => new WaybillSettings(user, a)));

				var suppliers = session.Query<Supplier>().ToList();
				var dirMaps = session.Query<DirMap>().ToList();
				var newDirMaps = suppliers
					.Except(dirMaps.Select(m => m.Supplier))
					.Select(s => new DirMap(settings, s))
					.ToArray();
				session.SaveEach(newDirMaps);
				transaction.Commit();
			}
			return false;
		}

		public void UpgradeData()
		{
			using (var session = Factory.OpenSession())
			using (var transaction = session.BeginTransaction()) {
				var markups = session.Query<MarkupConfig>().ToList();
				markups
					.Where(c => c.MaxMarkup < c.Markup)
					.Each(c => c.MaxMarkup = c.Markup);
				transaction.Commit();
			}
		}

		public void UpgradeSchema()
		{
			var export = new SchemaUpdate(Configuration);
			export.Execute(Debug, true);

			//nhibernate не проверяет типы данных и размерность, делаем еще проход для проверки типов данных
			using (var connectionProvider = ConnectionProviderFactory.NewConnectionProvider(Configuration.Properties))
			using (var connection = (DbConnection) connectionProvider.GetConnection()) {
				var cmd = connection.CreateCommand();
				var defaultCatalog = PropertiesHelper.GetString(NHibernate.Cfg.Environment.DefaultCatalog, Configuration.Properties, null);
				var defaultSchema = PropertiesHelper.GetString(NHibernate.Cfg.Environment.DefaultSchema, Configuration.Properties, null);
				var dialect = Dialect.GetDialect(Configuration.Properties);
				var tables = Tables().Where(t => t.IsPhysicalTable && Configuration.IncludeAction(t.SchemaActions, SchemaAction.Update));
				var meta = new DatabaseMetadata(connection, dialect);
				var mapping = (IMapping)Configuration.GetType().GetField("mapping", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Configuration);
				foreach (var table in tables) {
					var tableMeta = meta.GetTableMetadata(table.Name,
						table.Schema ?? defaultSchema,
						table.Catalog ?? defaultCatalog,
						table.IsQuoted);
					if (tableMeta == null)
						continue;

					var alters = new List<string>();
					var root = new StringBuilder("alter table ")
						.Append(table.GetQualifiedName(dialect, defaultCatalog, defaultSchema))
						.Append(" MODIFY ");
					foreach (var column in table.ColumnIterator) {
						var columnInfo = tableMeta.GetColumnMetadata(column.Name);
						if (columnInfo == null) {
							continue;
						}

						var diff = !CompareType(columnInfo, dialect, column, mapping);
						if (diff) {
							var sql = new StringBuilder()
								.Append(root)
								.Append(column.GetQuotedName(dialect))
								.Append(" ")
								.Append(column.GetSqlType(dialect, mapping));
							if (!string.IsNullOrEmpty(column.DefaultValue)) {
								sql.Append(" default ").Append(column.DefaultValue);

								if (column.IsNullable) {
									sql.Append(dialect.NullColumnString);
								}
								else {
									sql.Append(" not null");
								}
							}

							var useUniqueConstraint = column.Unique && dialect.SupportsUnique
								&& (!column.IsNullable || dialect.SupportsNotNullUnique);
							if (useUniqueConstraint) {
								sql.Append(" unique");
							}

							if (column.HasCheckConstraint && dialect.SupportsColumnCheck) {
								sql.Append(" check(").Append(column.CheckConstraint).Append(") ");
							}

							var columnComment = column.Comment;
							if (columnComment != null) {
								sql.Append(dialect.GetColumnComment(columnComment));
							}
							alters.Add(sql.ToString());
						}
					}

					if (table.Name.Match("Offers")){
						if (tableMeta.GetIndexMetadata("ProductSynonym") == null) {
							alters.Add("alter table Offers add fulltext (ProductSynonym);");
						}
						//из-за ошибки в 0.15.0, были созданы дублирующие индексы чистим их
						var schema = new DevartMySqlSchema(connection);
						var indexes = schema.GetIndexInfo(tableMeta.Catalog, null, tableMeta.Name).AsEnumerable()
							.Select(x => new DevartMySQLIndexMetadata(x))
							.Where(x => Regex.IsMatch(x.Name, @"ProductSynonym_\d+", RegexOptions.IgnoreCase))
							.ToArray();
						foreach (var index in indexes)
							alters.Add(String.Format("alter table {0} drop index {1};", tableMeta.Name, index.Name));
					}

					if (alters.Count > 0) {
						foreach (var alter in alters) {
							try {
								if (Log.IsDebugEnabled)
									Log.Debug(alter);
								cmd.CommandText = alter;
								cmd.ExecuteNonQuery();
							}
							catch(Exception e) {
								Log.Warn("Ошибка при обновлении схемы", e);
							}
						}
					}
				}//foreach
			}//using
		}

		private static bool CompareType(IColumnMetadata columnInfo, Dialect dialect, Column column, IMapping mapping)
		{
			if (Convert.ToBoolean(columnInfo.Nullable) != column.IsNullable)
				return false;
			var typeName = dialect.GetTypeName(column.GetSqlTypeCode(mapping));
			typeName = typeName.Replace(" UNSIGNED", "");
			if (typeName.Match("INTEGER")) {
				typeName = "int";
			}
			typeName = new Regex(@"\(\d+(\s*,\s*\d+)?\)$").Replace(typeName, "");
			return columnInfo.TypeName.Match(typeName);
		}

		public void InitDb()
		{
			var export = new SchemaExport(Configuration);
			export.Drop(Debug, true);
			export.Create(Debug, true);
			using (var connectionProvider = ConnectionProviderFactory.NewConnectionProvider(Configuration.Properties))
			using (var connection = (DbConnection) connectionProvider.GetConnection()) {
				var cmd = connection.CreateCommand();
				var alters = new List<string> {
					"alter table Offers add fulltext (ProductSynonym);"
				};
				foreach (var alter in alters) {
					try {
						if (Log.IsDebugEnabled)
							Log.Debug(alter);
						cmd.CommandText = alter;
						cmd.ExecuteNonQuery();
					}
					catch(Exception e) {
						Log.Warn("Ошибка при обновлении схемы", e);
					}
				}
			}
		}
	}
}