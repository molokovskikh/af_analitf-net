#if DEBUG
using System;
using System.Diagnostics;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using NHibernate.AdoNet.Util;
using Test.Support.log4net;

namespace AnalitF.Net.Client.ViewModels
{
	public class DebugViewModel : Screen, IAppender
	{
		private SimpleLayout layout = new SimpleLayout();
		private int limit = 10000;

		public DebugViewModel()
		{
			layout.ActivateOptions();
			Error = "";
			Sql = "";
			SqlCount = new NotifyValue<int>();
			ErrorCount = new NotifyValue<int>();
			HaveErrors = new NotifyValue<bool>(() => ErrorCount.Value > 0, ErrorCount);

			//нужно вызвать иначе wpf игнорирует все настройки протоколирование
			PresentationTraceSources.Refresh();

			PresentationTraceSources.DataBindingSource.Listeners.Add(new DelegateTraceListner(m => {
				ErrorCount.Value++;
				Error += m + "\r\n";
			}));

			var catcherSql = new QueryCatcher();
			catcherSql.Appender = this;
			catcherSql.Start();

			var catcher = new QueryCatcher("AnalitF.Net.Client");
			catcher.Level = Level.Warn;
			catcher.Appender = this;
			catcher.Start();
		}

		public string Name { get; set; }

		public string Error { get; set; }
		public string Sql { get; set; }

		public NotifyValue<int> ErrorCount { get; set; }
		public NotifyValue<bool> HaveErrors { get; set; }
		public NotifyValue<int> SqlCount { get; set; }

		public void Close()
		{
		}

		public void DoAppend(LoggingEvent loggingEvent)
		{
			if (loggingEvent.LoggerName == "NHibernate.SQL") {
				SqlCount.Value++;
				var sql = (string)loggingEvent.MessageObject;
				if (Sql.Length > limit)
					Sql = "";
				Sql += new BasicFormatter().Format(SqlProcessor.ExtractArguments(sql)) + "\r\n";
			}
			else {
				ErrorCount.Value++;
				if (Error.Length > limit)
					Error = "";
				Error += layout.Format(loggingEvent) + "\r\n";
			}
		}
	}
}
#endif