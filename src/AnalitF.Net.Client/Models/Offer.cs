using System.Collections.Generic;
using System.ComponentModel;
using AnalitF.Net.Client.Config.Initializers;

namespace AnalitF.Net.Client.Models
{
	public class Offer : INotifyPropertyChanged
	{
		private decimal? _diff;
		private uint orderCount;

		public virtual ulong Id { get; set; }

		public virtual ulong RegionId { get; set;  }

		public virtual string RegionName { get; set; }

		//public virtual uint PriceId { get; set; }

		public virtual Price Price { get; set; }

		public virtual uint ProductId { get; set; }

		public virtual uint CatalogId { get; set; }

		public virtual uint ProductSynonymId { get; set; }

		public virtual uint? ProducerId { get; set; }

		public virtual uint? ProducerSynonymId { get; set; }

		public virtual string Code { get; set; }

		public virtual string CodeCr { get; set; }

		public virtual string Unit { get; set; }

		public virtual string Volume { get; set; }

		public virtual string Quantity { get; set; }

		public virtual string Note { get; set; }

		public virtual string Period { get; set; }

		public virtual string Doc { get; set; }

		public virtual bool Junk { get; set; }

		public virtual decimal? MinBoundCost { get; set; }

		public virtual bool VitallyImportant { get; set; }

		public virtual uint? RequestRatio { get; set; }

		public virtual decimal? RegistryCost { get; set; }

		public virtual decimal? MaxBoundCost { get; set; }

		public virtual decimal? OrderCost { get; set; }

		public virtual uint? MinOrderCount { get; set; }

		public virtual decimal? ProducerCost { get; set; }

		public virtual uint? NDS { get; set; }

		public virtual string EAN13 { get; set; }

		public virtual string CodeOKP { get; set; }

		public virtual string Series { get; set; }

		public virtual string ProductSynonym { get; set; }

		public virtual string ProducerSynonym { get; set; }

		public virtual decimal Cost { get; set; }

		public virtual Price LeaderPrice { get; set; }

		public virtual decimal LeaderCost { get; set; }

		public virtual ulong LeaderRegionId { get; set; }

		public virtual string LeaderRegion { get; set; }

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

		public virtual OrderLine Line { get; set; }

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
				if (Line == null) {
					if (Price.Order == null) {
						Price.Order = new Order(Price);
					}
					Line = new OrderLine(Price.Order, this);
					Price.Order.AddLine(Line);
				}
				Line.Count = orderCount;
			}
			if (orderCount == 0 && Line != null) {
				Price.Order.RemoveLine(Line);
				Line = null;
				if (Price.Order.IsEmpty) {
					Price.Order = null;
				}
			}
			return Price.Order;
		}
	}
}