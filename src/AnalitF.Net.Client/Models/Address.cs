using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace AnalitF.Net.Client.Models
{
	public class Address
	{
		private BindingList<Order> bindable;

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

		[IgnoreDataMember]
		public virtual BindingList<Order> BindableOrders
		{
			get
			{
				if (bindable == null) {
					bindable = new BindingList<Order>(Orders);
				}
				return bindable;
			}
		}

		public override string ToString()
		{
			return string.Format("{0} {1}", Id, Name);
		}
	}
}