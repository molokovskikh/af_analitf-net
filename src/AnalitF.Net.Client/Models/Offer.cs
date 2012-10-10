using System.Collections.Generic;
using System.ComponentModel;
using AnalitF.Net.Client.Config.Initializers;

namespace AnalitF.Net.Client.Models
{
	public class Offer : BaseOffer, INotifyPropertyChanged
	{
		private decimal? _diff;
		private uint orderCount;

		public virtual ulong Id { get; set; }

		public virtual ulong RegionId { get; set;  }

		public virtual string RegionName { get; set; }

		public virtual Price Price { get; set; }

		public virtual Price LeaderPrice { get; set; }

		public virtual decimal LeaderCost { get; set; }

		public virtual ulong LeaderRegionId { get; set; }

		public virtual string LeaderRegion { get; set; }

		public virtual OrderLine OrderLine { get; set; }

		public virtual bool Leader
		{
			get { return LeaderRegionId == RegionId && LeaderPrice.Id == Price.Id; }
		}

		[Ignore]
		public virtual decimal? Diff
		{
			get { return _diff; }
		}

		[Ignore]
		public virtual int SortKeyGroup { get; set; }

		[Ignore]
		public virtual decimal RetailCost { get; set; }

		public virtual void CalculateDiff(decimal cost)
		{
			if (cost == Cost || cost == 0)
				return;

			_diff = (Cost - cost) / cost;
		}

		public virtual void CalculateRetailCost(List<MarkupConfig> markups)
		{
			var markup = MarkupConfig.Calculate(markups, this);
			RetailCost = Cost * (1 + markup / 100);
		}

		[Ignore]
		public virtual uint OrderCount
		{
			get { return orderCount; }
			set
			{
				orderCount = value;
				OnPropertyChanged("OrderCount");
				OnPropertyChanged("OrderSum");
			}
		}

		[Ignore]
		public virtual decimal OrderSum
		{
			get { return OrderCount * Cost; }
			set { }
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public virtual Order UpdateOrderLine()
		{
			if (OrderCount > 0) {
				if (OrderLine == null) {
					if (Price.Order == null) {
						Price.Order = new Order(Price);
					}
					OrderLine = new OrderLine(Price.Order, this);
					Price.Order.AddLine(OrderLine);
				}
				OrderLine.Count = orderCount;
			}
			if (orderCount == 0 && OrderLine != null) {
				Price.Order.RemoveLine(OrderLine);
				OrderLine = null;
				if (Price.Order.IsEmpty) {
					Price.Order = null;
				}
			}
			return Price.Order;
		}
	}
}