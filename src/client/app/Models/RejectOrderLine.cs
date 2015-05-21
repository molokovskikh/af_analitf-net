using System;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Models
{
	public class OrderReject
	{
		public OrderReject()
		{
			Lines = new List<OrderRejectLine>();
		}

		public virtual uint Id { get; set; }
		public virtual uint DownloadId { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual DateTime WriteTime { get; set; }
		public virtual IList<OrderRejectLine> Lines { get; set; }
	}

	public class OrderRejectLine
	{
		public virtual uint Id { get; set; }
		public virtual OrderReject OrderReject { get; set; }
		public virtual string Code { get; set; }
		public virtual string Product { get; set; }
		public virtual uint? ProductId { get; set; }
		public virtual uint? CatalogId { get; set; }
		public virtual string Producer { get; set; }
		public virtual uint? ProducerId { get; set; }
		public virtual uint Count { get; set; }
	}
}