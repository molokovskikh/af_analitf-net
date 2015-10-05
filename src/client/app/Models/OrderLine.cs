using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Newtonsoft.Json;
using NHibernate;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public class OrderLine : BaseOffer, IInlineEditable, IOrderLine, IFormattable
	{
		private uint count;
		private string comment;

		public OrderLine()
		{
		}

		public OrderLine(Order order, Offer offer, uint count)
			: base(offer)
		{
			Order = order;
			OfferId = offer.Id;
			Count = count;
			OptimalFactor = offer.ResultCost - offer.LeaderCost;
		}

		public virtual uint Id { get; set; }

		public virtual string SendError { get; set; }

		public virtual LineResultStatus SendResult { get; set; }

		[JsonIgnore]
		public virtual uint? ExportOrderId { get; set; }

		[JsonIgnore]
		public virtual uint? ExportId { get; set; }

		[JsonIgnore]
		public virtual uint? ExportBatchLineId { get; set; }

		[JsonIgnore]
		public virtual Order Order { get; set; }

		public virtual decimal ResultCost
		{
			get
			{
				if (Order == null)
					return Cost;
				return GetResultCost(Order.Price);
			}
		}

		public virtual decimal MixedCost
		{
			get
			{
				return HideCost ? ResultCost : Cost;
			}
		}

		public virtual decimal? NewCost { get; set; }

		public virtual decimal? MixedNewCost
		{
			get { return GetResultCost(Order.Price, NewCost); }
		}

		public virtual decimal? OldCost { get; set; }

		public virtual decimal? MixedOldCost
		{
			get { return GetResultCost(Order.Price, OldCost); }
		}

		[JsonIgnore]
		public virtual decimal? OptimalFactor { get; set; }

		[Ignore]
		public virtual decimal? MinCost { get; set; }

		[Ignore]
		public virtual PriceComposedId MinPrice { get; set; }

		[Ignore]
		public virtual decimal? LeaderCost { get; set; }

		[Ignore]
		public virtual PriceComposedId LeaderPrice { get; set; }

		public virtual uint? NewQuantity { get; set; }

		public virtual uint? OldQuantity { get; set;  }

		[Style("MixedNewCost", "MixedOldCost")]
		public virtual bool IsCostChanged
		{
			get { return (SendResult & LineResultStatus.CostChanged) > 0; }
		}

		[Style("MixedNewCost", "MixedOldCost")]
		public virtual bool IsCostIncreased
		{
			get { return (SendResult & LineResultStatus.CostChanged) > 0 && NewCost > OldCost; }
		}

		[Style("MixedNewCost", "MixedOldCost")]
		public virtual bool IsCostDecreased
		{
			get { return (SendResult & LineResultStatus.CostChanged) > 0 && NewCost < OldCost; }
		}

		[Style("NewQuantity", "OldQuantity")]
		public virtual bool IsQuantityChanged
		{
			get { return (SendResult & LineResultStatus.QuantityChanged) > 0; }
		}

		[Style("ProductSynonym", Context = "Correction")]
		public virtual bool IsRejected
		{
			get {
				return SendResult != LineResultStatus.OK
					&& (SendResult & LineResultStatus.CostChanged) == 0
					&& (SendResult & LineResultStatus.QuantityChanged) == 0;
			}
		}

		[Style("Order.AddressName")]
		public virtual bool IsCurrentAddress
		{
			get { return Order.IsCurrentAddress; }
		}

		[Style("Sum", Description = "Корректировка по цене и/или по количеству", Context = "CorrectionEnabled")]
		public virtual bool IsSendError
		{
			get { return SendResult != LineResultStatus.OK; }
		}

		//заглушка что бы работало смешение цветов от IsSendError
		[Style("Sum")]
		public virtual bool SelectCount
		{
			get { return !IsSendError; }
		}

		public virtual string Comment
		{
			get { return comment;}
			set
			{
				comment = value;
				OnPropertyChanged();
			}
		}

		public virtual uint Count
		{
			get { return count; }
			set
			{
				count = value;
				OnPropertyChanged();
				OnPropertyChanged("Sum");
				OnPropertyChanged("ResultSum");
				OnPropertyChanged("MixedSum");
			}
		}

		public virtual OfferComposedId OfferId { get; set; }

		/// <summary>
		/// сумма по строке без отсрочки платежа, нужно для вычислений
		/// </summary>
		public virtual decimal Sum
		{
			get { return Count * Cost; }
		}

		/// <summary>
		/// сумма по строке с отсрочкой платежа, нужно для отображения в предложениях
		/// </summary>
		public virtual decimal ResultSum
		{
			get { return Count * ResultCost; }
		}

		/// <summary>
		/// сумма по строке с отсрочкой платежа или без в зависимости от настройки Отображать реальную цену поставщика
		/// нужно для отображения в заказах
		/// </summary>
		public virtual decimal MixedSum
		{
			get { return Count * MixedCost; }
		}

		public override decimal GetResultCost()
		{
			return ResultCost;
		}

		public virtual List<Message> EditValidate()
		{
			var result = new List<Message>();
			if (Count == 0)
				return result;

			if (Count > 65535) {
				Count = 65535;
			}

			var quantity = SafeConvert.ToUInt32(Quantity);
			if (quantity > 0 && Count > quantity) {
				Count = CalculateAvailableQuantity(Count);
				result.Add(Message.Error(String.Format("Заказ превышает остаток на складе, товар будет заказан в количестве {0}", Count)));
			}

			if (Count > 1000)
				result.Add(Message.Warning("Внимание! Вы заказали большое количество препарата."));

			return result;
		}

		public virtual List<Message> SaveValidate(uint lastValidCount = 0, Func<string, bool> confirmCallback = null)
		{
			var result = new List<Message>();
			if (Count == 0)
				return result;

			if (BuyingMatrixType == BuyingMatrixStatus.Warning
				&& (confirmCallback == null
					|| !confirmCallback("Препарат не входит в разрешенную матрицу закупок.\r\nВы действительно хотите заказать его?"))) {
				Count = 0;
				return result;
			}

			string error = null;
			if (Count % SafeRequestRatio != 0) {
				error = String.Format("Поставщиком определена кратность по заказываемой позиции.\r\nВведенное значение \"{0}\" не кратно установленному значению \"{1}\"",
					Count,
					RequestRatio);
			}
			else if (MinOrderSum != null && Sum < MinOrderSum) {
				error = String.Format("Сумма заказа \"{0}\" меньше минимальной сумме заказа \"{1}\" по данной позиции!",
					Sum,
					MinOrderSum);
			}
			else if (MinOrderCount != null && Count < MinOrderCount) {
				error = String.Format("'Заказанное количество \"{0}\" меньше минимального количества \"{1}\" по данной позиции!'",
					Count,
					MinOrderCount);
			}

			if (!String.IsNullOrEmpty(error)) {
				Count = CalculateAvailableQuantity(Count);
				if (Count == 0) {
					Count = lastValidCount;
				}
				result.Add(Message.Error(error));
			}
			return result;
		}

		public virtual bool IsCountValid()
		{
			return Count == CalculateAvailableQuantity(Count);
		}

		public virtual uint CalculateAvailableQuantity(uint quantity)
		{
			if (Cost == 0)
				return quantity;
			var topBound = SafeConvert.ToUInt32(Quantity);
			if (topBound == 0)
				topBound = uint.MaxValue;
			topBound = Math.Min(topBound, quantity);
			var bottomBound = Math.Ceiling(MinOrderSum.GetValueOrDefault() / Cost);
			bottomBound = Math.Max(MinOrderCount.GetValueOrDefault(), bottomBound);
			var result = topBound - (topBound % SafeRequestRatio);
			if (result < bottomBound)
				return 0;
			return result;
		}

		[Ignore, JsonIgnore]
		public virtual uint Value
		{
			get { return Count; }
			set { Count = value; }
		}

		public virtual void Merge(Order order, Offer[] offers, StringBuilder log)
		{
			var rest = Count;
			foreach (var offer in offers) {
				if (rest == 0)
					break;

				var line = order.Lines.FirstOrDefault(l => l.OfferId == offer.Id);
				if (line == null) {
					line = new OrderLine(order, offer, rest);
					line.Count = line.CalculateAvailableQuantity(line.Count);
					if (line.Count > 0)
						order.AddLine(line);
					rest = rest - line.Count;
				}
				else {
					var toOrder = line.Count + rest;
					line.Count = line.CalculateAvailableQuantity(toOrder);
					rest = toOrder - line.Count;
				}
			}

			Order.RemoveLine(this);

			if (rest > 0) {
				if (rest == Count) {
					log.AppendLine(String.Format("{0} : {1} ; Предложений не найдено",
						order.Price.Name,
						this));
				}
				else {
					log.AppendLine(String.Format("{0} : {1} ; Уменьшено заказное количество {2} вместо {3}",
						Order.Price.Name,
						this,
						Count - rest,
						Count));
				}
			}
		}

		public virtual void Apply(OrderLineResult result)
		{
			ResetStatus();
			if (result == null) {
				return;
			}
			SendResult = result.Result;
			if (IsCostChanged) {
				NewCost = result.ServerCost;
				OldCost = Cost;
			}
			if (IsQuantityChanged) {
				NewQuantity = result.ServerQuantity;
				OldQuantity = NullableConvert.ToUInt32(Quantity);
			}
			if (result.ServerLineId != null) {
				ExportId = result.ServerLineId;
			}
			HumanizeSendError();
		}

		private void ResetStatus()
		{
			SendResult = LineResultStatus.OK;
			SendError = "";
			NewCost = null;
			OldCost = null;
			NewQuantity = null;
			OldQuantity = null;
		}

		public virtual void HumanizeSendError()
		{
			if (SendResult == LineResultStatus.CostChanged) {
				SendError = "имеется различие в цене препарата";
			}
			else if (SendResult == LineResultStatus.QuantityChanged) {
				SendError = "доступное количество препарата в прайс-листе меньше заказанного ранее";
			}
			else if (SendResult == (LineResultStatus.QuantityChanged | LineResultStatus.CostChanged)) {
				SendError = "имеются различия с прайс-листом в цене и количестве заказанного препарата";
			}
			else if (SendResult == LineResultStatus.NoOffers) {
				SendError = "предложение отсутствует";
			}
			else if (SendResult == LineResultStatus.CountReduced) {
				SendError = "Уменьшено заказное количество";
			}
			else {
				SendError = "";
			}
		}

		public override string ToString()
		{
			return String.Format("{0} - {1}", ProductSynonym, ProducerSynonym);
		}

		public virtual string ToString(string format, IFormatProvider formatProvider)
		{
			switch (format) {
				case "r":
					return String.Join(" ", ToString() + ":", LongSendError);
				default:
					return ToString();
			}
		}

		[JsonIgnore]
		public virtual string LongSendError
		{
			get
			{
				if (string.IsNullOrEmpty(SendError))
					return "";
				var datum = new List<string>();
				if (IsCostChanged)
					datum.Add(String.Format("старая цена: {0:C}", MixedOldCost));
				if (IsQuantityChanged)
					datum.Add(String.Format("старый заказ: {0:C}", OldQuantity));
				if (IsCostChanged)
					datum.Add(String.Format("новая цена: {0:C}", MixedNewCost));
				if (IsQuantityChanged)
					datum.Add(String.Format("текущий заказ: {0:C}", NewQuantity));

				var data = "";
				if (datum.Count > 0)
					data = "(" + datum.Implode("; ") + ")";
				return String.Join(" ", SendError, data);
			}
		}

		public static string SendReport(IEnumerable<OrderLine> lines, bool groupByAddress)
		{
			var offset = "    ";
			var currentOffset = "";
			var builder = new StringBuilder();
			var addressGroups = lines.GroupBy(l => l.Order.Address);
			foreach (var addressGroup in addressGroups) {
				if (groupByAddress)
					builder.AppendFormat("адрес доставки {0}", addressGroup.Key.Name)
						.AppendLine();
				foreach (var group in addressGroup.GroupBy(l => l.Order)) {
					if (groupByAddress)
						currentOffset = offset;
					builder
						.AppendFormat(currentOffset)
						.AppendFormat("прайс-лист {0}", group.Key.PriceName);
					if (group.Key.SendResult == OrderResultStatus.Reject)
						builder.AppendFormat(" - {0}", group.Key.SendError);
					builder.AppendLine();
					currentOffset += offset;
					foreach (var orderLine in group) {
						builder
							.Append(currentOffset)
							.AppendLine(String.Format("{0:r}", orderLine));
					}
				}
			}
			return builder.ToString();
		}

		public static string RestoreReport(IEnumerable<Order> orders)
		{
			var offset = "    ";
			var currentOffset = "";
			var builder = new StringBuilder();
			var addressGroups = orders
				.Where(o => o.SendResult != OrderResultStatus.OK || o.Lines.Any(l => l.SendResult != LineResultStatus.OK))
				.GroupBy(l => l.Address);
			foreach (var addressGroup in addressGroups) {
				try {
					builder.AppendFormat("адрес доставки {0}", addressGroup.Key.Name).AppendLine();
				}
				catch(ObjectNotFoundException) {
					builder.AppendFormat("адрес доставки <адрес доставки удален>").AppendLine();
				}
				foreach (var order in addressGroup) {
					currentOffset = offset;
					builder
						.AppendFormat(currentOffset)
						.AppendFormat("прайс-лист {0}", order.PriceName);
					if (order.SendResult != OrderResultStatus.OK)
						builder.AppendFormat(" - {0}", order.SendError);
					builder.AppendLine();
					currentOffset += offset;
					foreach (var orderLine in order.Lines.Where(l => l.SendResult != LineResultStatus.OK)) {
						builder
							.Append(currentOffset)
							.AppendLine(String.Format("{0:r}", orderLine));
					}
				}
			}
			return builder.ToString();
		}
	}
}