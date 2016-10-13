using System;
using System.Diagnostics;
using log4net;

namespace AnalitF.Net.Client.Helpers
{
	public class Log4netTraceListner : TraceListener
	{
		ILog log = LogManager.GetLogger(typeof(Log4netTraceListner));

		public override void TraceEvent(TraceEventCache eventCache, string source,
			TraceEventType eventType, int id, string format, params object[] args)
		{
			if (eventType == TraceEventType.Error || eventType == TraceEventType.Critical) {
				//ошибки ввода нет смысла протоколировать
				if (format.IndexOf("ConvertBack cannot convert value ", StringComparison.InvariantCulture) >= 0)
					return;
				log.ErrorFormat(format, args);
			}
		}

		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
		}
	}

	public class DelegateTraceListner : TraceListener
	{
		public Action<string> action;

		public DelegateTraceListner(Action<string> action)
		{
			this.action = action;
		}

		public override void TraceEvent(TraceEventCache eventCache, string source,
			TraceEventType eventType, int id, string format, params object[] args)
		{
			if (eventType == TraceEventType.Error || eventType == TraceEventType.Critical) {
				//ошибки ввода нет смысла протоколировать
				if (format.IndexOf("ConvertBack cannot convert value ", StringComparison.InvariantCulture) >= 0)
					return;
				action(String.Format(format, args));
			}
		}

		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
		}
	}
}