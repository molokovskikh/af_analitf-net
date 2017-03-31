#if DEBUG
using System;
using System.Reactive.Linq;
using System.Diagnostics;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using NHibernate.AdoNet.Util;
using Test.Support.log4net;

namespace AnalitF.Net.Client.ViewModels
{
	public class Debug : Screen, IAppender
	{
		private LayoutSkeleton layout = new PatternLayout("%d{dd.MM.yyyy HH:mm:ss.fff} [%t] %-5p %c - %m%n%exception%n");
		private int limit = 100000;

		public Debug()
		{
			layout.ActivateOptions();
			Error = "";
			Stack = new NotifyValue<bool>();
			Sql = new NotifyValue<string>("");
			SqlCount = new NotifyValue<int>();
			ErrorCount = new NotifyValue<int>();
			HaveErrors = ErrorCount.Select(c => c > 0).ToValue();

			PresentationTraceSources.DataBindingSource.Listeners.Add(new DelegateTraceListner(m => {
				ErrorCount.Value++;
				Error += m + "\r\n";
			}));

			var catcherSql = new QueryCatcher();
			catcherSql.Appender = this;
			catcherSql.Start();

			var repository = (Hierarchy)log4net.LogManager.GetRepository();
			var logger = (Logger)repository.GetLogger("AnalitF.Net.Client");
			if (logger.Level > Level.Warn) {
				logger.Level = Level.Warn;
			}
			logger.AddAppender(this);

			var caliburncatcher = new QueryCatcher("Caliburn.Micro");
			caliburncatcher.Additivity = true;
			caliburncatcher.Level = Level.Warn;
			caliburncatcher.Appender = this;
			caliburncatcher.Start();
		}

		public string Name { get; set; }

		public string Error { get; set; }
		public NotifyValue<string> Sql { get; set; }

		public NotifyValue<int> ErrorCount { get; set; }
		public NotifyValue<bool> HaveErrors { get; set; }
		public NotifyValue<int> SqlCount { get; set; }
		public NotifyValue<bool> Stack { get; set; }

		public void Clear()
		{
			Sql.Value = "";
			SqlCount.Value = 0;
		}

		public void Close()
		{
		}

		public void DoAppend(LoggingEvent loggingEvent)
		{
			if (loggingEvent.LoggerName == "NHibernate.SQL") {
				SqlCount.Value++;
				var sql = (string)loggingEvent.MessageObject;
				if (Sql.Value.Length > limit)
					Sql.Value = "";
				// BasicFormatter выбрасывает исключение при попытке форматировать некоторые запросы, например, вида "(select a from b)"
				Sql.Value = new BasicFormatter().Format(SqlProcessor.ExtractArguments(sql)) + Environment.NewLine + Sql.Value;
				if (Stack)
					Sql.Value = new StackTrace() + Environment.NewLine + Sql.Value;
			}
			else {
				if (loggingEvent.Level < Level.Warn)
					return;
				ErrorCount.Value++;
				if (Error.Length > limit)
					Error = "";
				Error += layout.Format(loggingEvent) + Environment.NewLine;
			}
		}
	}
}
#endif