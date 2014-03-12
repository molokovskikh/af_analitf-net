﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using Common.NHibernate;
using Common.Tools;
using Devart.Data.MySql;
using Iesi.Collections;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Dialect.Schema;
using NHibernate.Engine;
using NHibernate.Exceptions;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using log4net;
using NHibernate.Util;

namespace AnalitF.Net.Client.Models
{
	public class SanityCheck : BaseCommand
	{
		public static bool Debug;

		public void Check(bool updateSchema = false)
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
				//http://dev.mysql.com/doc/refman/4.1/en/error-messages-server.html
				if (e.InnerException is MySqlException &&
					(((MySqlException)e.InnerException).Code == 1054
						|| ((MySqlException)e.InnerException).Code == 1146)) {
					crushOnFirstTry = true;
					log.Error("База данных повреждена попробую восстановить", e);
				}
				else {
					throw;
				}
			}

			//если свалились на первой попытке нужно попытаться починить базу и попробовать еще разик
			if (crushOnFirstTry) {
				UpgradeSchema();
				CheckSettings(true);
			}
		}

		private bool CheckSettings(bool overrideHash)
		{
			using (var session = Factory.OpenSession())
			using (var transaction = session.BeginTransaction()) {
				var settings = session.Query<Settings>().FirstOrDefault();
				var mappingToken = AppBootstrapper.NHibernate.MappingHash;
				if (settings == null) {
					settings = new Settings(defaults: true, token: mappingToken);
					session.Save(settings);
				}
				else {
					if (overrideHash)
						settings.MappingToken = mappingToken;

					if (settings.MappingToken != mappingToken)
						return true;
					//проверяем что данные корректны и если не корректны
					//пытаемся восстановить их
					if (settings.Markups.Count == 0)
						session.Query<MarkupConfig>().Each(settings.AddMarkup);

					//если ничего восстановить не удалось тогда берем значения по умолчанию
					if (settings.Markups.Count == 0)
						MarkupConfig.Defaults().Each(settings.AddMarkup);

					if (settings.Waybills.Count == 0)
						session.Query<WaybillSettings>().Each(settings.Waybills.Add);
				}

				//если есть адреса то должен быть и пользователь
				//если только база не была поломана
				var user = session.Query<User>().FirstOrDefault()
					?? new User();

				var addresses = session.Query<Address>().ToList();
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
				session.Query<MarkupConfig>()
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

						//если тип задан явно то сравнить его не получится
						if (!String.IsNullOrEmpty(column.SqlType))
							continue;

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

					if (alters.Count > 0) {
						foreach (var alter in alters) {
							try {
								cmd.CommandText = alter;
								cmd.ExecuteNonQuery();
							}
							catch(Exception e) {
								log.Warn("Ошибка при обновлении схемы", e);
							}
						}
					}
				}//foreach
			}//using
		}

		private static bool CompareType(IColumnMetadata columnInfo, Dialect dialect, Column column, IMapping mapping)
		{
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
		}
	}
}