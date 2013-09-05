using System;
using System.Diagnostics;

namespace AnalitF.Net.Client.Helpers
{
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
			if (eventType == TraceEventType.Error || eventType == TraceEventType.Critical)
				action(String.Format(format, args));
		}

		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
		}
	}
}