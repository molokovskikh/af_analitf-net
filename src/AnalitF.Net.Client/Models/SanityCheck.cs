using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Commands;
using Common.NHibernate;
using Common.Tools;
using Devart.Data.MySql;
using Iesi.Collections;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Exceptions;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using log4net;

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
		}

		public void InitDb()
		{
			var export = new SchemaExport(Configuration);
			export.Drop(Debug, true);
			export.Create(Debug, true);
		}
	}
}