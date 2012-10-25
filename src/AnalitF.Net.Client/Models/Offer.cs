using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.Initializers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class Offer : BaseOffer, INotifyPropertyChanged
	{
		private decimal totalOrderSum;

		private decimal? _diff;
		private uint orderCount;
		private decimal? prevOrderAvgCost;
		private decimal? prevOrderAvgCount;

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

		public virtual void CalculateDiff(decimal cost)
		{
			if (cost == Cost || cost == 0)
				return;

			_diff = Math.Round((Cost - cost) / cost, 2);
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
		}

		[Ignore]
		public virtual string Warning { get; set; }

		[Ignore]
		public virtual string Notification { get; set; }

		//Значение для этого поля загружается исинхронно, что бы ui узнал о загрузке надо его оповестить
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
		public virtual decimal TotalOrderSum
		{
			get { return totalOrderSum; }
			set {
				totalOrderSum = value;
				OnPropertyChanged("TotalOrderSum");
			}
		}



		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public virtual Order UpdateOrderLine(Address address)
		{
			var order = Price.Order;
			if (OrderCount > 0) {
				PreorderCheck();
				if (OrderLine == null) {
					if (order == null) {
						order = new Order(Price, address);
						Price.Order = order;
					}
					OrderLine = new OrderLine(order, this, orderCount);
					order.AddLine(OrderLine);
				}
				else {
					OrderLine.Count = OrderCount;
					OrderLine.Order.Sum = OrderLine.Order.Lines.Sum(l => l.Sum);
				}
			}
			if (orderCount == 0 && OrderLine != null) {
				order.RemoveLine(OrderLine);
				OrderLine = null;
				if (order.IsEmpty) {
					Price.Order = null;
				}
				Warning = null;
			}
			return order;
		}

		private void PreorderCheck()
		{
			Notification = Check();
			OrderCount = CalculateAvailableQuantity(OrderCount);
		}

		private uint CalculateAvailableQuantity(uint quantity)
		{
			var topBound = SafeConvert.ToUInt32(Quantity);
			if (topBound == 0)
				topBound = uint.MaxValue;
			topBound = Math.Min(topBound, quantity);
			var bottomBound = Math.Ceiling(MinOrderSum.GetValueOrDefault() / Cost);
			bottomBound = Math.Max(MinOrderCount.GetValueOrDefault(), bottomBound);
			var result = topBound - (topBound % RequestRatio.GetValueOrDefault(1));
			if (result < bottomBound) {
				return 0;
			}
			return result;
		}

		public virtual string Check()
		{
			if (OrderCount == 0)
				return null;

			if (OrderCount % RequestRatio.GetValueOrDefault(1) != 0) {
				return String.Format("Поставщиком определена кратность по заказываемой позиции.\r\nВведенное значение \"{0}\" не кратно установленному значению \"{1}\"",
					OrderCount,
					RequestRatio);
			}
			if (MinOrderSum != null && OrderSum < MinOrderSum) {
				return String.Format("Сумма заказа \"{0}\" меньше минимальной сумме заказа \"{1}\" по данной позиции!",
					OrderSum,
					MinOrderSum);
			}

			if (MinOrderCount != null && OrderCount < MinOrderCount) {
				return String.Format("'Заказанное количество \"{0}\" меньше минимального количества \"{1}\" по данной позиции!'",
					OrderCount,
					MinOrderCount);
			}

			//проверка матрицы
			if (false) {
				return "Препарат не входит в разрешенную матрицу закупок.\r\nВы действительно хотите заказать его?";
			}
			return null;
		}

		public virtual void MakePreorderCheck()
		{
			Warning = null;
			Notification = null;
			if (OrderCount == 0)
				return;

			if (OrderCount > 65535) {
				OrderCount = 65535;
			}

			var quantity = SafeConvert.ToUInt32(Quantity);
			if (quantity > 0 && OrderCount > quantity) {
				OrderCount = CalculateAvailableQuantity(OrderCount);
				Notification = String.Format("Заказ превышает остаток на складе, товар будет заказан в количестве {0}", OrderCount);
			}

			var warnings = new List<string>();
			if (Junk)
				warnings.Add("Вы заказали препарат с ограниченным сроком годности\r\nили с повреждением вторичной упаковки.");

			if (OrderCount > 1000) {
				warnings.Add("Внимание! Вы заказали большое количество препарата.");
			}

			if (false) {
				warnings.Add("Товар присутствует в замороженных заказах.");
			}

			if (false) {
				warnings.Add("Превышение среднего заказа!");
			}

			if (false) {
				warnings.Add("Превышение средней цены!");
			}

			Warning = warnings.Implode(Environment.NewLine);
		}
	}
}