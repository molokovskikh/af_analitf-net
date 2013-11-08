using System;

namespace AnalitF.Net.Client.Helpers
{
	public class FailFastLog : Caliburn.Micro.ILog
	{
		public FailFastLog(Type type)
		{
		}

		public void Info(string format, params object[] args)
		{
		}

		public void Warn(string format, params object[] args)
		{
		}

		public void Error(Exception exception)
		{
			throw new Exception("Ошибка при выполнении операции", exception);
		}
	}
}