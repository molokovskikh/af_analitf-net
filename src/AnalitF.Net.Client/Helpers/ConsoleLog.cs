using System;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Helpers
{
	public class ConsoleLog : ILog
	{
		public void Info(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}

		public void Warn(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}

		public void Error(Exception exception)
		{
			Console.WriteLine(exception);
		}
	}
}