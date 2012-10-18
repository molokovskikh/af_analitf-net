using System;

namespace AnalitF.Net.Client.Models
{
	public class Price
	{
		public virtual uint Id { get; set; }

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

		public virtual Order Order { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}