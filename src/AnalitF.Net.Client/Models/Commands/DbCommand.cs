using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using NHibernate;
using NHibernate.Cfg;
using log4net;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.Models.Commands
{
	public class BaseCommand
	{
		protected ILog log;
		protected Configuration Configuration;
		protected ISessionFactory Factory;

		public CancellationToken Token;
		public ISession Session;
		public IStatelessSession StatelessSession;
		public Config.Config Config = new Config.Config();
		public ProgressReporter Reporter;
		public BehaviorSubject<Progress> Progress;

		protected BaseCommand()
		{
			Progress = new BehaviorSubject<Progress>(new Progress());
			Reporter = new ProgressReporter(Progress);
			log = LogManager.GetLogger(GetType());
			if (AppBootstrapper.NHibernate != null) {
				Configuration = AppBootstrapper.NHibernate.Configuration;
				Factory = AppBootstrapper.NHibernate.Factory;
			}
		}

		protected T RunCommand<T>(DbCommand<T> command)
		{
			command.Session = Session;
			command.StatelessSession = StatelessSession;
			command.Token = Token;
			command.Reporter = Reporter;
			command.Execute();
			return command.Result;
		}

		protected IEnumerable<Table> Tables()
		{
			var dialect = NHibernate.Dialect.Dialect.GetDialect(Configuration.Properties);
			var tables = Configuration.CreateMappings(dialect).IterateTables;
			return tables;
		}

		protected IEnumerable<string> TableNames()
		{
			return Tables().Select(t => t.Name);
		}
	}

	public abstract class DbCommand<T> : BaseCommand
	{
		public T Result;

		public abstract void Execute();
	}

	public abstract class DbCommand : DbCommand<object>
	{
	}
}