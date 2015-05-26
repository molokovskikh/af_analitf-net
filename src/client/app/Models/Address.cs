using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Config.Initializers;
using ReactiveUI;

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
			StatSubject = new Subject<Stat>();
			YesterdayOrders = new List<Tuple<uint, uint>>();
		}

		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual bool HaveLimits { get; set; }

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

		[IgnoreDataMember, Ignore]
		public virtual Subject<Stat> StatSubject { get; protected set; }

		[IgnoreDataMember, Ignore]
		public virtual List<Tuple<uint, uint>> YesterdayOrders { get; set; }

		public override string ToString()
		{
			return string.Format("{0} {1}", Id, Name);
		}

		public virtual bool RemoveLine(OrderLine line)
		{
			var order = line.Order;
			if (order != null) {
				order.RemoveLine(line);
				if (order.IsEmpty)
					order.Address.Orders.Remove(order);
				else
					order.UpdateStat();
				StatSubject.OnNext(new Stat(this));
			}
			return line.Order.IsEmpty;
		}

		public virtual OrderLine Order(Offer offer, uint count)
		{
			var order = new Order(this, offer, count);
			Orders.Add(order);
			return order.Lines[0];
		}

		public virtual IEnumerable<Order> ActiveOrders()
		{
			return Orders.Where(o => !o.Frozen);
		}
	}
}