using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Cfg;
using log4net;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.Models.Commands
{
	public class BaseCommand : IDisposable
	{
		protected CompositeDisposable Disposable = new CompositeDisposable();
		protected ILog Log;
		protected Configuration Configuration;
		protected ISessionFactory Factory;

		public CancellationToken Token;
		public ISession Session;
		public IStatelessSession StatelessSession;
		public Config.Config Config;
		public ProgressReporter Reporter;
		public BehaviorSubject<Progress> Progress;

		protected BaseCommand()
		{
			Progress = new BehaviorSubject<Progress>(new Progress());
			Reporter = new ProgressReporter(Progress);
			Log = LogManager.GetLogger(GetType());
			if (AppBootstrapper.NHibernate != null) {
				Configuration = AppBootstrapper.NHibernate.Configuration;
				Factory = AppBootstrapper.NHibernate.Factory;
			}
		}

		protected void EnsureInit()
		{
			if (StatelessSession == null)
				Disposable.Add(StatelessSession = Factory.OpenStatelessSession());
			if (Session == null)
				Disposable.Add(Session = Factory.OpenSession());
		}

		protected T RunCommand<T>(DbCommand<T> command)
		{
			Configure(command);
			command.Execute();
			return command.Result;
		}

		protected T Configure<T>(T command) where T : BaseCommand
		{
			command.Session = Session;
			command.StatelessSession = StatelessSession;
			command.Token = Token;
			command.Reporter = Reporter;
			command.Config = Config;
			return command;
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

		public void Dispose()
		{
			Disposable.Dispose();
		}
	}

	public abstract class DbCommand<T> : BaseCommand
	{
		public T Result;

		public abstract void Execute();

		public Task<T> ToTask()
		{
			var task = new Task<T>(() => {
				using (this) {
					Execute();
					return Result;
				}
			});
			return task;
		}
	}

	public abstract class DbCommand : DbCommand<object>
	{
	}
}