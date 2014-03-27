using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class MailSendLog
	{
		public virtual uint Id { get; set; }
		public virtual uint MailId { get; set; }
		public virtual User User { get; set; }
	}

	public class PendingMailLog
	{
		public PendingMailLog()
		{
		}

		public PendingMailLog(MailSendLog log)
		{
			WriteTime = DateTime.Now;
			SendLog = log;
			User = log.User;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime WriteTime { get; set; }
		public virtual MailSendLog SendLog { get; set; }
		public virtual User User { get; set; }
	}
}