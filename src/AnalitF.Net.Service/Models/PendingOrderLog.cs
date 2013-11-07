using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class PendingOrderLog
	{
		public PendingOrderLog()
		{
		}

		public PendingOrderLog(Order order, User user, uint exportId)
		{
			WriteTime = DateTime.Now;
			Order = order;
			User = user;
			ExportId = exportId;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime WriteTime { get; set; }
		public virtual User User { get; set; }
		public virtual Order Order { get; set; }
		public virtual uint ExportId { get; set; }
	}
}