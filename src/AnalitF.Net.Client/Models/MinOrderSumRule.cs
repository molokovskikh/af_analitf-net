using System;

namespace AnalitF.Net.Client.Models
{
	public class MinOrderSumRule : IEquatable<MinOrderSumRule>
	{
		public MinOrderSumRule()
		{
		}

		public MinOrderSumRule(Address address, Price price, decimal minOrderSum)
		{
			Address = address;
			Price = price;
			MinOrderSum = minOrderSum;
			IsRuleMandatory = true;
		}

		public virtual Address Address { get; set; }
		public virtual Price Price { get; set; }
		public virtual decimal MinOrderSum { get; set; }
		public virtual bool IsRuleMandatory { get; set; }

		public virtual bool Equals(MinOrderSumRule other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(Price, other.Price) && Equals(Address, other.Address);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((MinOrderSumRule)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return ((Price != null ? Price.Id.GetHashCode() : 0) * 397) ^ (Address != null ? Address.Id.GetHashCode() : 0);
			}
		}
	}
}