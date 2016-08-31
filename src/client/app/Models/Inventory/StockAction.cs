using System;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class StockAction : StockActionAttrs
	{
		public virtual uint Id { get; set; }
		public virtual DateTime Timestamp { get; set; }

		[Ignore]
		public virtual Stock Stock { get; set; }
	}
}