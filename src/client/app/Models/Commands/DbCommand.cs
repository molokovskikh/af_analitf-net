using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Common.Tools;
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

		public void InitSession()
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

		public void ProcessBatch(uint[] ids, Action<ISession, IEnumerable<uint>> process)
		{
			using (var session = Factory.OpenSession())
			using (var trx = session.BeginTransaction()) {
				foreach (var page in ids.Page(100))
					process(session, page);
				trx.Commit();
			}
		}
	}

	public abstract class DbCommand<T> : BaseCommand
	{
		public T Result;

		public abstract void Execute();

		public Task ToTask(Config.Config config, CancellationToken token = default(CancellationToken))
		{
			Config = config;
			Token = token;
			var task = new Task(() => {
				using (this) {
					InitSession();
					Execute();
				}
			});
			return task;
		}
	}

	public abstract class DbCommand : DbCommand<object>
	{
	}
}