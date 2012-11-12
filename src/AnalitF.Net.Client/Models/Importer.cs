using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;

namespace AnalitF.Net.Client.Models
{
	public class Importer
	{
		private ISession session;

		public Importer(ISession session)
		{
			this.session = session;
		}

		public void Import(List<System.Tuple<string, string[]>> tables)
		{
			var export = new SchemaExport(AppBootstrapper.NHibernate.Configuration);
			export.Drop(false, true);
			export.Create(false, true);

			foreach (var table in tables) {
				try {
					var sql = String.Format("LOAD DATA INFILE '{0}' INTO TABLE {1} ({2})",
						table.Item1,
						Path.GetFileNameWithoutExtension(table.Item1),
						table.Item2.Implode());
					var dbCommand = session.Connection.CreateCommand();
					dbCommand.CommandText = sql;
					dbCommand.ExecuteNonQuery();
				}
				catch (Exception e) {
					throw new Exception(String.Format("Не могу импортировать {0}", table), e);
				}
			}

			new SanityCheck().Check();

			var settings = session.Query<Settings>().ToList()[0];
			settings.ApplyChanges(session);
		}
	}
}
