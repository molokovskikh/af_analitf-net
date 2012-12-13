﻿using System;
using AnalitF.Net.Client.Config.Initializers;
using Remotion.Linq.Utilities;

namespace AnalitF.Net.Client.Models
{
	[Serializable]
	public class PriceComposedId : IEquatable<PriceComposedId>, IComparable
	{
		public uint PriceId { get; set; }

		public ulong RegionId { get; set; }

		public bool Equals(PriceComposedId other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return RegionId == other.RegionId && PriceId == other.PriceId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((PriceComposedId)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return (RegionId.GetHashCode() * 397) ^ (int)PriceId;
			}
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
				return 1;

			var id = obj as PriceComposedId;
			if (id == null)
				throw new ArgumentTypeException("obj", typeof(PriceComposedId), obj.GetType());

			if (PriceId != id.PriceId)
				if (PriceId > id.PriceId)
					return 1;
				else
					return -1;

			if (RegionId != id.RegionId)
				if (RegionId > id.RegionId)
					return 1;
				else
					return -1;

			return 0;
		}

		public static bool operator ==(PriceComposedId v1, PriceComposedId v2) {
			return Equals(v1, v2);
		}

		public static bool operator !=(PriceComposedId v1, PriceComposedId v2) {
			return !Equals(v1, v2);
		}

		public override string ToString()
		{
			return string.Format("PriceId: {0}, RegionId: {1}", PriceId, RegionId);
		}
	}

	public class Price
	{
		public virtual PriceComposedId Id { get; set; }

		/// <summary>
		/// название которое отображается в интерфейсе, зависит от опции "Всегда показывать названия прайс-листов"
		/// </summary>
		public virtual string Name { get; set; }

		/// <summary>
		/// Название прайса без названия поставщика, нужно для вычисления, Name
		/// не использовать, используй Name
		/// </summary>
		public virtual string PriceName { get; set; }

		public virtual ulong RegionId { get; set; }

		public virtual string RegionName { get; set; }

		public virtual uint SupplierId { get; set; }

		public virtual string SupplierName { get; set; }

		public virtual string SupplierFullName { get; set; }

		public virtual bool Storage { get; set; }

		public virtual uint PositionCount { get; set; }

		public virtual DateTime PriceDate { get; set; }

		public virtual string OperativeInfo { get; set; }

		public virtual string ContactInfo { get; set; }

		public virtual string Phone { get; set; }

		public virtual string Email { get; set; }

		public virtual uint MinReq { get; set; }

		public virtual bool BasePrice { get; set; }

		public virtual int Category { get; set; }

		[Ignore]
		public virtual Order Order { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}