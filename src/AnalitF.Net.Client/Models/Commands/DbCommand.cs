using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NHibernate;
using NHibernate.Cfg;
using log4net;

namespace AnalitF.Net.Client.Models.Commands
{
	public abstract class DbCommand<T>
	{
		protected ILog log;
		protected Configuration configuration;
		protected ISessionFactory factory;
		protected string dataPath;

		public CancellationToken Token;
		public ISession Session;
		public T Result;

		protected DbCommand()
		{
			log = LogManager.GetLogger(GetType());
			configuration = AppBootstrapper.NHibernate.Configuration;
			factory = AppBootstrapper.NHibernate.Factory;
			dataPath = AppBootstrapper.DataPath;
		}

		public abstract void Execute();

		public static IEnumerable<string> Tables(Configuration configuration)
		{
			var dialect = NHibernate.Dialect.Dialect.GetDialect(configuration.Properties);
			var tables = configuration.CreateMappings(dialect).IterateTables.Select(t => t.Name);
			return tables;
		}
	}

	public abstract class DbCommand : DbCommand<object>
	{
	}
}