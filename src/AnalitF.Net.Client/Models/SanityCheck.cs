using System.IO;
using System.Linq;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;

namespace AnalitF.Net.Client.Models
{
	public class SanityCheck
	{
		private string dataPath;

		public SanityCheck(string dataPath)
		{
			this.dataPath = dataPath;
		}

		public void Check(bool updateSchema = false)
		{
			var factory = AppBootstrapper.NHibernate.Factory;
			var configuration = AppBootstrapper.NHibernate.Configuration;

			if (!Directory.Exists(dataPath)) {
				Directory.CreateDirectory(dataPath);
				InitDb(configuration);
			}
			else if (updateSchema) {
				UpdateDb(configuration);
			}

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
			}
		}

		private static void UpdateDb(Configuration configuration)
		{
			var export = new SchemaUpdate(configuration);
			export.Execute(false, true);
		}

		private static void InitDb(Configuration configuration)
		{
			var export = new SchemaExport(configuration);
			export.Drop(false, true);
			export.Create(false, true);
		}
	}
}