using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Proxy;
using Newtonsoft.Json.Serialization;

namespace AnalitF.Net.Client.Models
{
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

		public Order(Address address, Offer offer)
			: this(offer.Price, address)
		{
			AddLine(offer, 1);
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
				if (frozen == value)
					return;
				frozen = value;
				ResetStatus();
				OnPropertyChanged("Frozen");
			}
		}

		public virtual string Comment { get; set; }

		public virtual string PersonalComment { get; set; }

		public virtual OrderResultStatus SendResult { get; set; }

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

		[Style("Sum", Description = "Имеется позиция с корректировкой по цене и/или по количеству")]
		public virtual bool IsOrderLineSendError
		{
			get { return Lines.Any(l => l.IsSendError); }
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

		public virtual void ResetStatus()
		{
			SendError = "";
			SendResult = OrderResultStatus.OK;
			Lines.Each(l => l.Apply(null));
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

		public virtual bool IsAccepted
		{
			get { return SendResult == OrderResultStatus.OK && ServerId > 0; }
		}

		public virtual void Apply(OrderResult result)
		{
			result = result ?? new OrderResult { Result = OrderResultStatus.Reject };

			SendResult = result.Result;
			SendError = result.Error;
			ServerId = result.ServerOrderId;

			if (SendResult != OrderResultStatus.OK
				&& String.IsNullOrEmpty(SendError)) {
				SendError = "Неизвестная ошибка сервера";
			}

			if (SendResult != OrderResultStatus.OK) {
				Lines.Each(l => l.Apply(result.Lines.FirstOrDefault(r => r.ClientLineId == l.Id)));
			}
		}
	}
}