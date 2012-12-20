using System.Collections.Generic;

namespace AnalitF.Net.Client.Models
{
	public class Address
	{
		public Address()
		{
			Orders = new List<Order>();
			Rules = new List<MinOrderSumRule>();
		}

		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual IList<MinOrderSumRule> Rules { get; set; }

		public virtual IList<Order> Orders { get; set; }
	}
}