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
			User = log.User;
			SendLog = log;
		}

		public virtual uint Id { get; set; }
		public virtual DocumentSendLog SendLog { get; set; }
		public virtual User User { get; set; }
	}
}