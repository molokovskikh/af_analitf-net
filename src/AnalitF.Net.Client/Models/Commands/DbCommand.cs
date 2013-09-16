using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NHibernate;
using NHibernate.Cfg;
using log4net;

namespace AnalitF.Net.Client.Models.Commands
{
	public class BaseCommand
	{
		protected ILog log;
		protected Configuration Configuration;
		protected ISessionFactory Factory;
		protected string DataPath;

		public CancellationToken Token;
		public ISession Session;
		public IStatelessSession StatelessSession;

		protected BaseCommand()
		{
			log = LogManager.GetLogger(GetType());
			Configuration = AppBootstrapper.NHibernate.Configuration;
			Factory = AppBootstrapper.NHibernate.Factory;
			DataPath = AppBootstrapper.DataPath;
		}

		protected T RunCommand<T>(DbCommand<T> command)
		{
			command.Session = Session;
			command.StatelessSession = StatelessSession;
			command.Token = Token;
			command.Execute();
			return command.Result;
		}
	}

	public abstract class DbCommand<T> : BaseCommand
	{
		public T Result;

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