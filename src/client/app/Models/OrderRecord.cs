using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.Models
{
	class OrderRecord
	{
		public OrderRecord()
		{
			ImortTime = DateTime.Now;
		}

		public OrderRecord(uint orderId, uint? keepId) : this()
		{
			OrderId = orderId;
			KeepId = keepId;
			IsImported = true;
		}

		public virtual uint Id { get; set; }

		public virtual uint OrderId { get; set; }

		public virtual uint? KeepId { get; set; }

		public virtual bool IsImported { get; set; }

		public virtual DateTime ImortTime { get; set; }

	}
}
