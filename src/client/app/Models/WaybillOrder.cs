using System;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Models
{
	public class WaybillOrder : IEquatable<WaybillOrder>
	{
		public WaybillOrder(uint documentLineId, uint orderLineId)
		{
			DocumentLineId = documentLineId;
			OrderLineId = orderLineId;
		}

		public WaybillOrder()
		{
		}

		public virtual uint DocumentLineId { get; set; }
		public virtual uint OrderLineId { get; set; }

		public virtual bool Equals(WaybillOrder other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return DocumentLineId == other.DocumentLineId && OrderLineId == other.OrderLineId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != this.GetType())
				return false;
			return Equals((WaybillOrder)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return ((int)DocumentLineId * 397) ^ (int)OrderLineId;
			}
		}
	}
}