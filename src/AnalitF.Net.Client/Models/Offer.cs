using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.Initializers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	[Serializable]
	public class OfferComposedId
	{
		public ulong OfferId { get; set; }

		public ulong RegionId { get; set; }

		protected bool Equals(OfferComposedId other)
		{
			return OfferId == other.OfferId && RegionId == other.RegionId;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((OfferComposedId)obj);
		}

		public override int GetHashCode()
		{
			unchecked {
				return ((int)OfferId * 397) ^ RegionId.GetHashCode();
			}
		}

		public static bool operator ==(OfferComposedId v1, OfferComposedId v2) {
			return Equals(v1, v2);
		}

		public static bool operator !=(OfferComposedId v1, OfferComposedId v2) {
			return !Equals(v1, v2);
		}

		public override string ToString()
		{
			return string.Format("OfferId: {0}, RegionId: {1}", OfferId, RegionId);
		}
	}

	public class Offer : BaseOffer, INotifyPropertyChanged
	{
		private decimal? _diff;
		private uint? orderCount;
		private decimal? prevOrderAvgCost;
		private decimal? prevOrderAvgCount;
		private OrderLine orderLine;

		public virtual OfferComposedId Id { get; set; }

		public virtual ulong RegionId { get; set;  }

		public virtual string RegionName { get; set; }

		public virtual Price Price { get; set; }

		public virtual Price LeaderPrice { get; set; }

		public virtual decimal LeaderCost { get; set; }

		public virtual ulong LeaderRegionId { get; set; }

		public virtual string LeaderRegion { get; set; }

		public virtual bool Leader
		{
			get { return LeaderPrice.Id == Price.Id; }
		}

		//Заглушка для реализации матрицы
		[Ignore]
		public virtual bool Banned { get; set; }

		[Ignore]
		public virtual OrderLine OrderLine
		{
			get { return orderLine; }
			set
			{
				orderLine = value;
				OnPropertyChanged("OrderLine");
			}
		}

		[Ignore]
		public virtual decimal? Diff
		{
			get { return _diff; }
		}

		//поле для отображения сгруппированные данных
		//оно здесь потомучто PropertyGroupDescription
		//может группировать только по свойству
		//а переделывать механиз группировки не целесообразно
		//используется если снята опция "Поиск по форме выпуска"
		[Ignore]
		public virtual string GroupName { get; set; }

		[Ignore]
		public virtual int SortKeyGroup { get; set; }

		public virtual void CalculateDiff(decimal cost)
		{
			if (cost == Cost || cost == 0)
				return;

			_diff = Math.Round((Cost - cost) / cost, 2);
		}

		[Ignore]
		public virtual uint? OrderCount
		{
			get { return orderCount; }
			set
			{
				orderCount = value;
				OnPropertyChanged("OrderCount");
				OnPropertyChanged("OrderSum");
			}
		}

		//Значение для этого поля загружается асинхронно, что бы ui узнал о загрузке надо его оповестить
		[Ignore]
		public virtual decimal? PrevOrderAvgCost
		{
			get
			{
				return prevOrderAvgCost;
			}
			set
			{
				prevOrderAvgCost = value;
				OnPropertyChanged("PrevOrderAvgCost");
			}
		}

		[Ignore]
		public virtual decimal? PrevOrderAvgCount
		{
			get { return prevOrderAvgCount; }
			set
			{
				prevOrderAvgCount = value;
				OnPropertyChanged("PrevOrderAvgCount");
			}
		}

		[Ignore]
		public virtual bool StatLoaded { get; set; }

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public virtual List<Message> UpdateOrderLine(Address address, Settings settings, string comment = null, bool edit = true)
		{
			var result = Enumerable.Empty<Message>().ToList();
			if (address == null) {
				OrderCount = null;
				return result;
			}
			var order = Price.Order;
			if (OrderCount.GetValueOrDefault(0) > 0) {
				if (OrderLine == null) {
					if (order == null) {
						order = new Order(Price, address);
						Price.Order = order;
						address.Orders.Add(order);
					}
					OrderLine = new OrderLine(order, this, orderCount.Value);
					orderLine.Comment = comment;
					order.AddLine(OrderLine);
				}
				else {
					OrderLine.Count = OrderCount.Value;
					OrderLine.Comment = comment;
					OrderLine.Order.Sum = OrderLine.Order.Lines.Sum(l => l.Sum);
				}

				if (edit) {
					result.AddRange(OrderLine.EditValidate());
					result.AddRange(EditValidate(address, settings));
				}
				else
					result = OrderLine.SaveValidate();
				OrderCount = OrderLine.Count;
			}

			if (OrderCount.GetValueOrDefault(0) == 0 && OrderLine != null) {
				order.RemoveLine(OrderLine);
				OrderLine = null;
				OrderCount = null;
				if (order.IsEmpty) {
					address.Orders.Remove(order);
					Price.Order = null;
				}
			}

			return result;
		}

		private List<Message> EditValidate(Address address, Settings settings)
		{
			var result = new List<Message>();
			if (OrderCount.GetValueOrDefault(0) == 0)
				return result;

			if (address.Orders.Where(o => o.Frozen).SelectMany(o => o.Lines).Any(l => l.ProductId == ProductId)) {
				result.Add(Message.Warning("Товар присутствует в замороженных заказах."));
			}

			if (PrevOrderAvgCount != null && OrderCount > PrevOrderAvgCount * settings.OverCountWarningFactor) {
				result.Add(Message.Warning("Превышение среднего заказа!"));
			}

			if (PrevOrderAvgCost != null && Cost > PrevOrderAvgCost * (1 + settings.OverCostWarningPercent / 100)) {
				result.Add(Message.Warning("Превышение средней цены!"));
			}
			return result;
		}

		public virtual void AttachOrderLine(OrderLine orderLine)
		{
			OrderLine = orderLine;
			Price.Order = orderLine.Order;
			OrderCount = orderLine.Count;
		}

		public virtual List<Message> SaveOrderLine(Address address, Settings settings, string comment = null)
		{
			return UpdateOrderLine(address, settings, comment, false);
		}
	}
}