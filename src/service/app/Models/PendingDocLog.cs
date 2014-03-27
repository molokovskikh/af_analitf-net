using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class PendingDocLog
	{
		public PendingDocLog()
		{
		}

		public PendingDocLog(DocumentSendLog log)
		{
			WriteTime = DateTime.Now;
			User = log.User;
			SendLog = log;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime WriteTime { get; set; }
		public virtual DocumentSendLog SendLog { get; set; }
		public virtual User User { get; set; }
	}
}