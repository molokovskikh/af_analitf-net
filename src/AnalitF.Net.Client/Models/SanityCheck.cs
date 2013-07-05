﻿using System;
using System.IO;
using System.Linq;
using Devart.Data.MySql;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Exceptions;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using log4net;

namespace AnalitF.Net.Client.Models
{
	public class SanityCheck
	{
		private string dataPath;
		private ILog log = LogManager.GetLogger(typeof(SanityCheck));

		public static bool Debug;
		private Configuration configuration;

		public SanityCheck(string dataPath)
		{
			this.dataPath = dataPath;
			configuration = AppBootstrapper.NHibernate.Configuration;
		}

		public void Check(bool updateSchema = false)
		{
			var factory = AppBootstrapper.NHibernate.Factory;
			if (!Directory.Exists(dataPath)) {
				Directory.CreateDirectory(dataPath);
				InitDb();
			}
			else if (updateSchema) {
				UpdateDb();
			}

			var crushOnFirstTry = false;
			try {
				CheckSettings(factory);
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
				UpdateDb();
				CheckSettings(factory);
			}
		}

		private void CheckSettings(ISessionFactory factory)
		{
			using (var session = factory.OpenSession()) {
				var settings = session.Query<Settings>().FirstOrDefault();
				if (settings == null) {
					session.Save(new Settings());
				}

				var markups = session.Query<MarkupConfig>().ToList();
				if (markups.Count == 0) {
					var defaults = MarkupConfig.Defaults();
					foreach (var markup in defaults) {
						session.Save(markup);
					}
				}

				var addresses = session.Query<Address>().ToList();
				if (addresses.Count > 0) {
					var user = session.Query<User>().First();
					var waybillSettings = session.Query<WaybillSettings>().ToList();
					foreach (var address in addresses) {
						var waybillSetting = waybillSettings.FirstOrDefault(s => s.BelongsToAddress == address);
						if (waybillSetting == null) {
							waybillSetting = new WaybillSettings(user, address);
							session.Save(waybillSetting);
						}
					}
				}
			}
		}

		public void UpdateDb()
		{
			var export = new SchemaUpdate(configuration);
			export.Execute(Debug, true);
		}

		public void InitDb()
		{
			var export = new SchemaExport(configuration);
			export.Drop(Debug, true);
			export.Create(Debug, true);
		}
	}
}