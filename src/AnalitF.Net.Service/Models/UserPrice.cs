using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class UserPrice : IEquatable<UserPrice>
	{
		public UserPrice()
		{
		}

		public UserPrice(User user, ulong regionId, PriceList price)
		{
			User = user;
			RegionId = regionId;
			Price = price;
		}

		public virtual User User { get; set; }
		public virtual PriceList Price { get; set; }
		public virtual ulong RegionId { get; set; }

		public virtual bool Equals(UserPrice other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(User, other.User) && Equals(Price, other.Price) && RegionId == other.RegionId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((UserPrice)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				int hashCode = (User != null ? User.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Price != null ? Price.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ RegionId.GetHashCode();
				return hashCode;
			}
		}
	}
}