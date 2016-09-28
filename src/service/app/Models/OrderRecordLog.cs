using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public enum RecordType
	{
		Loaded = 1,
		Accepted = 2,
	}
	public class OrderRecordLog
	{
		public OrderRecordLog()
 		{
 			WriteTime = DateTime.Now;
 		}

 		public OrderRecordLog(Order order, User user, uint exportId, RecordType recordType) : this()
 		{
 			Order = order;
 			User = user;
 			ExportId = exportId;
 			RecordType = recordType;
 		}

 		public virtual uint Id { get; set; }

		public virtual Order Order { get; set; }

		public virtual User User { get; set; }

		public virtual uint ExportId { get; set; }

		public virtual RecordType RecordType { get; set; }

		public virtual DateTime WriteTime { get; set; }
	}
}
