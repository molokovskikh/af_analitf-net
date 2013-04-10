using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AnalitF.Net.Client.Models
{
	public class Address
	{
		public Address(string name)
			: this()
		{
			Name = name;
		}

		public Address()
		{
			Orders = new List<Order>();
			Rules = new List<MinOrderSumRule>();
		}

		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		[IgnoreDataMember]
		public virtual IList<MinOrderSumRule> Rules { get; set; }

		[IgnoreDataMember]
		public virtual IList<Order> Orders { get; set; }

		public override string ToString()
		{
			return string.Format("{0} {1}", Id, Name);
		}
	}
}