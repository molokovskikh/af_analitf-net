using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Config.Initializers;
using NHibernate;
using NHibernate.Proxy;
using Newtonsoft.Json.Serialization;

namespace AnalitF.Net.Client.Models
{
	public class ClientOrder
	{
		public uint ClientOrderId { get; set; }
		public uint PriceId { get; set; }
		public uint AddressId { get; set; }
		public ulong RegionId { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime PriceDate { get; set; }
		public string Comment { get; set; }

		public OrderLine[] Items { get; set; }
	}

	public class Order : INotifyPropertyChanged
	{
		private decimal sum;

		public Order()
		{
			Lines = new List<OrderLine>();
		}

		public Order(Price price, Address address)
			: this()
		{
			Address = address;
			Price = price;
			CreatedOn = DateTime.Now;
		}

		public virtual uint Id { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual Address Address { get; set; }

		public virtual Price Price { get; set; }

		public virtual int LinesCount { get; set; }

		public virtual decimal Sum
		{
			get { return sum; }
			set
			{
				sum = value;
				OnPropertyChanged("Sum");
				OnPropertyChanged("IsValid");
			}
		}

		public virtual decimal MonthlyOrderSum { get; set; }

		public virtual decimal WeeklyOrderSum { get; set; }

		public virtual bool Send { get; set; }

		public virtual bool Frozen { get; set; }

		public virtual string Comment { get; set; }

		public virtual string PersonalComment { get; set; }

		public virtual IList<OrderLine> Lines { get; set; }

		public virtual bool IsValid
		{
			get
			{
				var rule = Address.Rules.FirstOrDefault(r => r.Price.Id == Price.Id);
				if (rule == null)
					return true;
				return Sum >= rule.MinOrderSum;
			}
		}

		public virtual bool IsEmpty
		{
			get { return Lines.Count == 0; }
		}

		public virtual void RemoveLine(OrderLine line)
		{
			Lines.Remove(line);
			Sum = Lines.Sum(l => l.Sum);
			LinesCount = Lines.Count;
		}

		public virtual void AddLine(OrderLine line)
		{
			Lines.Add(line);
			Sum = Lines.Sum(l => l.Sum);
			LinesCount = Lines.Count;
		}

		public virtual void AddLine(Offer offer, uint count)
		{
			AddLine(new OrderLine(this, offer, count));
		}

		public virtual ClientOrder ToClientOrder()
		{
			if (Address == null || Price == null)
				return null;

			return new ClientOrder {
				ClientOrderId = Id,
				AddressId = Address.Id,
				CreatedOn = CreatedOn,
				PriceId = Price.Id.PriceId,
				RegionId = Price.Id.RegionId,
				PriceDate = Price.PriceDate,
				Comment = Comment,
				Items = Lines.ToArray(),
			};
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}