using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Client.Helpers
{
	public class ProcessHelper
	{
		public static bool UnitTesting;
		public static List<string> ExecutedProcesses = new List<string>();

		public static void Start(ProcessStartInfo info)
		{
			if (UnitTesting) {
				var result = new[] {
					info.FileName,
					info.Arguments,
					info.Verb
				}.Where(s => !String.IsNullOrEmpty(s)).Implode(" ");
				ExecutedProcesses.Add(result);
				return;
			}
			Process.Start(info);
		}
	}
}