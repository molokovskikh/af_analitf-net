using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Config.Initializers;
using Common.Tools;
using NHibernate.Cfg;
using Newtonsoft.Json;

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
		}

		public virtual uint Id { get; set; }

		public virtual string SendError { get; set; }

		public virtual LineResultStatus SendResult { get; set; }

		public virtual decimal? ServerCost { get; set; }

		public virtual uint? ServerQuantity { get; set; }

		[JsonIgnore]
		public virtual Order Order { get; set; }

		public virtual decimal? NewCost
		{
			get { return  IsCostChanged ? ServerCost : null; }
		}

		public virtual decimal? OldCost
		{
			get { return IsCostChanged ? Cost : (decimal?)null; }
		}

		[Style("NewCost", "OldCost")]
		public virtual bool IsCostChanged
		{
			get { return (SendResult & LineResultStatus.CostChanged) > 0; }
		}

		public virtual uint? NewQuantity
		{
			get { return IsQuantityChanged ? ServerQuantity : null; }
		}

		public virtual uint? OldQuantity
		{
			get { return IsQuantityChanged ? Count : (uint?)null; }
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

		[Style("Sum", Description = "Корректировка по цене и/или по количеству")]
		public virtual bool IsSendError
		{
			get { return SendResult != LineResultStatus.OK; }
		}

		public virtual string Comment
		{
			get { return comment;}
			set
			{
				comment = value;
				OnPropertyChanged("Comment");
			}
		}

		public virtual uint Count
		{
			get { return count; }
			set
			{
				count = value;
				OnPropertyChanged("Count");
				OnPropertyChanged("Sum");
			}
		}

		public virtual OfferComposedId OfferId { get; set; }

		public virtual decimal Sum
		{
			get
			{
				return Count * Cost;
			}
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

		public virtual List<Message> SaveValidate()
		{
			var result = new List<Message>();
			if (Count == 0)
				return result;

			string error = null;
			if (Count % RequestRatio.GetValueOrDefault(1) != 0) {
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
				result.Add(Message.Error(error));
			}
			//заготовка что бы не забыть
			//проверка матрицы
			//if (false) {
			//	return "Препарат не входит в разрешенную матрицу закупок.\r\nВы действительно хотите заказать его?";
			//}
			return result;
		}

		public virtual bool IsCountValid()
		{
			return Count == CalculateAvailableQuantity(Count);
		}

		public virtual uint CalculateAvailableQuantity(uint quantity)
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
			if (result == null) {
				SendResult = LineResultStatus.OK;
				SendError = "";
				ServerCost = null;
				ServerQuantity = null;
				return;
			}
			ServerCost = result.ServerCost;
			ServerQuantity = result.ServerQuantity;
			SendResult = result.Result;
			if (result.Result == LineResultStatus.CostChanged) {
				SendError = "имеется различие в цене препарата";
			}
			else if (result.Result == LineResultStatus.QuantityChanged) {
				SendError = "доступное количество препарата в прайс-листе меньше заказанного ранее";
			}
			else if (result.Result == (LineResultStatus.QuantityChanged | LineResultStatus.CostChanged)) {
				SendError = "имеются различия с прайс-листом в цене и количестве заказанного препарата";
			}
			else if (result.Result == LineResultStatus.NoOffers) {
				SendError = "предложение отсутствует";
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
					return String.Join(" ", new [] {
						ToString() + ":",
						LongSendError
					});
				default:
					return ToString();
			}
		}

		public virtual string LongSendError
		{
			get
			{
				var datum = new List<string>();
				if (IsCostChanged)
					datum.Add(String.Format("старая цена: {0:C}", OldCost));
				if (IsQuantityChanged)
					datum.Add(String.Format("старый заказ: {0:C}", OldQuantity));
				if (IsCostChanged)
					datum.Add(String.Format("новая цена: {0:C}", NewCost));
				if (IsQuantityChanged)
					datum.Add(String.Format("текущий заказ: {0:C}", NewCost));

				var data = "";
				if (datum.Count > 0)
					data = "(" + datum.Implode("; ") + ")";
				return String.Join(" ", SendError, data);
			}
		}

		public static string SendReport(IEnumerable<OrderLine> lines)
		{
			var builder = new StringBuilder();
			foreach (var group in lines.GroupBy(l => l.Order)) {
				builder.AppendFormat("прайс-лист {0}", group.Key.Price.Name);
				if (group.Key.SendResult == OrderResultStatus.Reject)
					builder.AppendFormat(" - {0}", group.Key.SendError);
				builder.AppendLine();
				foreach (var orderLine in group) {
					builder.AppendLine(String.Format("    {0:r}", orderLine));
				}
			}
			return builder.ToString();
		}
	}
}