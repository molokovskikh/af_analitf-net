using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Proxy;
using Newtonsoft.Json.Serialization;

namespace AnalitF.Net.Client.Models
{
	public class Limit
	{
		public virtual Address Address { get; set; }
		public virtual Price Price { get; set; }
		public virtual decimal Value { get; set; }

		public virtual bool Equals(Limit other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(Price, other.Price) && Equals(Address, other.Address);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Limit)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return ((Price != null ? Price.Id.GetHashCode() : 0) * 397) ^ (Address != null ? Address.Id.GetHashCode() : 0);
			}
		}
	}

	public static class OrderQuery
	{
		public static IQueryable<Order> ReadyToSend(this IQueryable<Order> query, Address address)
		{
			var id = address != null ? address.Id : 0;
			return query.Where(o => o.Address.Id == id && !o.Frozen && o.Send);
		}
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

		public Order(Address address, Offer offer, uint count = 1)
			: this(offer.Price, address)
		{
			TryOrder(offer, count);
		}

		public virtual uint Id { get; set; }

		public virtual uint DisplayId
		{
			get { return Id; }
		}

		public virtual uint? ExportId { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual Address Address { get; set; }

		public virtual Price Price { get; set; }

		public virtual int LinesCount
		{
			get { return linesCount; }
			set
			{
				linesCount = value;
				OnPropertyChanged();
			}
		}

		public virtual decimal Sum
		{
			get { return sum; }
			set
			{
				sum = value;
				OnPropertyChanged();
				OnPropertyChanged("IsInvalid");
				OnPropertyChanged("IsOverLimit");
			}
		}

		public virtual bool Send
		{
			get { return send; }
			set
			{
				if (send == value)
					return;
				send = value;
				OnPropertyChanged();
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
				if (frozen)
					Send = false;
				ResetStatus();
				OnPropertyChanged();
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

		public virtual Limit Limit { get; set; }

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

		[Style("Limit.Value", Description = "Превышение лимита")]
		public virtual bool IsOverLimit
		{
			get
			{
				if (Limit == null)
					return false;
				return Sum > Limit.Value;
			}
		}

		[Style("Sum", Description = "Имеется позиция с корректировкой по цене и/или по количеству", Context = "CorrectionEnabled")]
		public virtual bool IsOrderLineSendError
		{
			get { return Lines.Any(l => l.IsSendError); }
		}

		[Style("AddressName"), Ignore, JsonIgnore]
		public virtual bool IsCurrentAddress { get; set; }

		public virtual bool IsEmpty
		{
			get { return Lines.Count == 0; }
		}

		public virtual string PriceLabel
		{
			get { return PriceName; }
		}

		public virtual string PriceName
		{
			get
			{
				try
				{
					if (Price == null)
						return "";
					return Price.Name;
				}
				catch(ObjectNotFoundException) {
					return "";
				}
			}
		}

		public virtual string AddressName
		{
			get
			{
				try
				{
					if (Address == null)
						return "";
					return Address.Name;
				}
				catch(ObjectNotFoundException) {
					return "";
				}
			}
		}

		public virtual Price SafePrice
		{
			get
			{
				if (IsPriceExists())
					return Price;
				return new Price {
					Id = Price.Id
				};
			}
		}

		public virtual bool IsPriceExists()
		{
			bool priceNotFound;
			try {
				priceNotFound = Price == null || Price.Name == "";
			}
			catch (ObjectNotFoundException) {
				priceNotFound = true;
			}
			return !priceNotFound;
		}

		public virtual bool IsAddressExists()
		{
			bool addressNotFound;
			try {
				addressNotFound = Address == null || Address.Name == "";
			}
			catch (ObjectNotFoundException) {
				addressNotFound = true;
			}
			return !addressNotFound;
		}

		public virtual void ResetStatus()
		{
			SendError = "";
			SendResult = OrderResultStatus.OK;
			Lines.Each(l => l.Apply(null));
		}

		public virtual void UpdateStat()
		{
			Sum = Lines.Sum(l => l.Sum);
			LinesCount = Lines.Count;
		}

		public virtual void RemoveLine(OrderLine line)
		{
			Lines.Remove(line);
			UpdateStat();
		}

		public virtual void AddLine(OrderLine line)
		{
			Lines.Add(line);
			UpdateStat();
		}

		public virtual OrderLine TryOrder(Offer offer, uint count)
		{
			uint ordered;
			var line = TryOrder(offer, count, out ordered);
			if (count != ordered)
				throw new Exception(String.Format("Не удалось заказать позицию {0} заказывалось {1} заказано {2}", offer, count, ordered));
			return line;
		}

		public virtual OrderLine TryOrder(Offer offer, uint count, out uint ordered)
		{
			ordered = 0;
			if (offer.BuyingMatrixType == BuyingMatrixStatus.Denied)
				return null;

			var line = Lines.FirstOrDefault(l => l.OfferId == offer.Id);
			if (line == null) {
				line = new OrderLine(this, offer, count);
				line.Count = line.CalculateAvailableQuantity(line.Count);
				if (line.Count == 0)
					return null;
				ordered = line.Count;
				AddLine(line);
			}
			else {
				var originCount = line.Count += count;
				line.Count = line.CalculateAvailableQuantity(line.Count);
				ordered = count - (originCount - line.Count);
				if (ordered == 0)
					line = null;
			}
			return line;
		}

		public virtual ClientOrder ToClientOrder(ISession session)
		{
			if (Frozen)
				return null;
			if (!Send)
				return null;
			try {
				if (Address == null || Price == null) {
					Send = false;
					Frozen = true;
					return null;
				}
			}
			catch(ObjectNotFoundException) {
				Send = false;
				Frozen = true;
				return null;
			}

			foreach (var line in Lines) {
				var offers = session.Query<Offer>().Where(o => o.ProductId == line.ProductId
					&& o.ProducerId == line.ProducerId
					&& o.Junk == line.Junk)
					.ToArray();
				line.MinCost = offers.Select(o => (decimal?)o.ResultCost).MinOrDefault();
				if (line.MinCost != null) {
					if (line.MinCost == line.ResultCost) {
						line.MinPrice = line.Order.Price.Id;
					}
					else {
						line.MinPrice = offers.Where(o => o.ResultCost == line.MinCost)
							.Select(p => p.Price.Id)
							.FirstOrDefault();
					}
				}

				var baseOffers = offers.Where(o => o.Price.BasePrice).ToArray();
				line.LeaderCost = baseOffers.Select(o => (decimal?)o.ResultCost).MinOrDefault();
				if (line.LeaderCost != null) {
					if (line.LeaderCost == line.ResultCost) {
						line.LeaderPrice = line.Order.Price.Id;
					}
					else {
						line.LeaderPrice = baseOffers.Where(o => o.ResultCost == line.LeaderCost)
							.Select(p => p.Price.Id)
							.FirstOrDefault();
					}
				}
			}

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

			Lines.Each(l => l.Apply(result.Lines.FirstOrDefault(r => r.ClientLineId == l.Id)));
		}

		public virtual void CalculateStyle(Address address)
		{
			IsCurrentAddress = IsAddressExists() && Address.Id == address.Id;
		}
	}
}
