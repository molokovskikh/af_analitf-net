using System.Collections.Generic;
using System.Diagnostics;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class MemoryTraceListner : TraceListener
	{
		private List<string> traces;

		public MemoryTraceListner(List<string> traces)
		{
			this.traces = traces;
		}

		public override void Write(string message)
		{
			traces.Add(message);
		}

		public override void WriteLine(string message)
		{
			traces.Add(message);
		}
	}
}