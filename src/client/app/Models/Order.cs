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

		[Style("Sum", Description = "Имеется позиция с корректировкой по цене и/или по количеству", Context = "CorrectionEnabled")]
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
			try {
				if (Address == null || Price == null) {
					Send = false;
					return null;
				}
			}
			catch(ObjectNotFoundException) {
				Send = false;
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
	}
}
