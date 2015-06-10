using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class CachedCostKey
	{
		public virtual uint Id { get; set; }
		public virtual User User { get; set; }
		public virtual Client Client { get; set; }
		public virtual PriceList Price { get; set; }
		public virtual ulong RegionId { get; set; }
		public virtual DateTime Date { get; set; }
	}
}