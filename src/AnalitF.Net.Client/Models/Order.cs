using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Proxy;
using Newtonsoft.Json.Serialization;

namespace AnalitF.Net.Client.Models
{
	public class SyncRequest
	{
		public SyncRequest(object[] prices)
		{
			Prices = prices;
		}

		public SyncRequest(ClientOrder[] orders)
		{
			Orders = orders;
		}

		public object[] Prices;
		public ClientOrder[] Orders;
	}

	public class OrderResult
	{
		public uint ClientOrderId;
		public ulong ServerOrderId;
		public string Error;
	}

	public class ClientOrder
	{
		public uint ClientOrderId;
		public uint PriceId;
		public uint AddressId;
		public ulong RegionId;
		public DateTime CreatedOn;
		public DateTime PriceDate;
		public string Comment;

		public OrderLine[] Items;
	}

	public class Order : BaseNotify, IOrder
	{
		private decimal sum;
		private int linesCount;
		private bool send;
		private bool frozen;

		public Order()
		{
			Lines = new List<OrderLine>();
		}

		public Order(Price price, Address address)
			: this()
		{
			Send = true;
			Address = address;
			Price = price;
			CreatedOn = DateTime.Now;
			MinOrderSum = Address.Rules.FirstOrDefault(r => r.Price.Id == Price.Id);
		}

		public virtual uint Id { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual Address Address { get; set; }

		public virtual Price Price { get; set; }

		public virtual int LinesCount
		{
			get { return linesCount; }
			set
			{
				linesCount = value;
				OnPropertyChanged("LinesCount");
			}
		}

		public virtual decimal Sum
		{
			get { return sum; }
			set
			{
				sum = value;
				OnPropertyChanged("Sum");
				OnPropertyChanged("IsInvalid");
			}
		}

		public virtual decimal MonthlyOrderSum { get; set; }

		public virtual decimal WeeklyOrderSum { get; set; }

		public virtual bool Send
		{
			get { return send; }
			set
			{
				send = value;
				OnPropertyChanged("Send");
			}
		}

		[Style(Description = "\"Заморожен\"")]
		public virtual bool Frozen
		{
			get { return frozen; }
			set
			{
				frozen = value;
				OnPropertyChanged("Frozen");
			}
		}

		public virtual string Comment { get; set; }

		public virtual string PersonalComment { get; set; }

		public virtual string SendError { get; set; }

		public virtual IList<OrderLine> Lines { get; set; }

		IEnumerable<IOrderLine> IOrder.Lines
		{
			get { return Lines; }
		}

		public virtual MinOrderSumRule MinOrderSum { get; set; }

		[Ignore]
		public virtual ulong ServerId { get; set; }

		[Style("MinOrderSum.MinOrderSum", Description = "Не удовлетворяет минимальной сумме")]
		public virtual bool IsInvalid
		{
			get
			{
				if (MinOrderSum == null)
					return true;
				return Sum < MinOrderSum.MinOrderSum;
			}
		}

		public virtual bool IsEmpty
		{
			get { return Lines.Count == 0; }
		}

		public virtual string PriceLabel
		{
			get
			{
				if (Price == null)
					return null;
				return Price.ToString();
			}
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

		public virtual OrderLine AddLine(Offer offer, uint count)
		{
			var line = new OrderLine(this, offer, count);
			AddLine(line);
			return line;
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
	}
}