using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace AnalitF.Net.Client.Test.Acceptance
{
	public class WindbgLog
	{
		public string MT;
		public int Count;
		public int TotalSize;
		public string ClassName;

		public WindbgLog(string mt, int count, int totalSize, string className)
		{
			MT = mt;
			Count = count;
			TotalSize = totalSize;
			ClassName = className;
		}

		public override string ToString()
		{
			return ClassName;
		}
	}

	public class WindbgHelper
	{
		public static List<WindbgLog> GetHeapDump(Process process)
		{
			var log = Path.GetFullPath("windbg.log");
			File.WriteAllText("windbg.cmd", String.Format(".loadby sos clr\r\n.logopen {0}\r\n!dumpheap -type AnalitF.Net.Client.ViewModels\r\nqq", log));
			var commands = String.Format("$<{0}", Path.GetFullPath("windbg.cmd"));
			var arguments = String.Format("-p {0} -c \"{1}\"", process.Id, commands);
			var windbg = Process.Start(@"C:\Program Files (x86)\Debugging Tools for Windows (x86)\windbg.exe", arguments);

			windbg.WaitForExit();
			Thread.Sleep(100);
			var logs = ParseLogs(log);
			return logs;
		}

		public static List<WindbgLog> ParseLogs(string file)
		{
			var reg = new Regex(@"^(?<mt>[a-z\d]+)\s+(?<count>\d+)\s+(?<size>\d+)\s(?<name>.+)$");
			var result = new List<WindbgLog>();
			foreach (var line in File.ReadLines(file)) {
				var match = reg.Match(line);
				if (match.Success) {
					result.Add(new WindbgLog(
						match.Groups["mt"].Value,
						Convert.ToInt32(match.Groups["count"].Value),
						Convert.ToInt32(match.Groups["size"].Value),
						match.Groups["name"].Value));
				}
			}
			return result;
		}
	}
}