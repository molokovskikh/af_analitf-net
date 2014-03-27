using System;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Helpers
{
	public class Log4net : ILog
	{
		private log4net.ILog logger;

		public Log4net(Type type)
		{
			logger = log4net.LogManager.GetLogger(type);
		}

		public void Info(string format, params object[] args)
		{
			logger.InfoFormat(format, args);
		}

		public void Warn(string format, params object[] args)
		{
			logger.WarnFormat(format, args);
		}

		public void Error(Exception exception)
		{
			logger.Error(exception);
		}
	}
}