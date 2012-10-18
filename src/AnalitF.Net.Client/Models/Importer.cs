using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

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
			foreach (var table in tables) {
				var sql = String.Format("LOAD DATA INFILE '{0}' INTO TABLE {1} ({2})",
					table.Item1,
					Path.GetFileNameWithoutExtension(table.Item1),
					table.Item2.Implode());
				var dbCommand = session.Connection.CreateCommand();
				dbCommand.CommandText = sql;
				dbCommand.ExecuteNonQuery();
			}

			new SanityCheck().Check();

			var settings = session.Query<Settings>().ToList()[0];
			settings.ApplyChanges(session);
		}
	}
}
