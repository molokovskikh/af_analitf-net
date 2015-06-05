using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public enum BuyingMatrixStatus
	{
		Allow = 0,
		Denied = 1,
		Warning = 2
	}

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

	public class Offer : BaseOffer, IInlineEditable
	{
		private decimal? _diff;
		private uint? orderCount;
		private decimal? prevOrderAvgCost;
		private decimal? prevOrderAvgCount;
		private OrderLine orderLine;

		public Offer()
		{
		}

		public Offer(Price price, decimal cost)
		{
			Id = new OfferComposedId();
			Id.RegionId = price.Id.RegionId;
			Id.OfferId = (ulong)base.GetHashCode();
			PriceId = price.Id.PriceId;
			Price = price;
			Cost = cost;
		}

		public Offer(Offer offer, decimal cost)
		{
			Clone(offer);
			Id = new OfferComposedId();
			Id.OfferId = offer.Id.OfferId++;
			Price = offer.Price;
			Cost = cost;
		}

		public Offer(Price price, Offer clone, decimal cost)
			: this(clone, cost)
		{
			Id = new OfferComposedId();
			Id.RegionId = price.Id.RegionId;
			PriceId = price.Id.PriceId;
			Price = price;
		}

		public virtual OfferComposedId Id { get; set; }

		//это поле нужно что бы сделать хибер счастливым
		//в тестах я создаю данные но без этого поля вставка не будет работать
		//ключ прайс листа состоит из двух полей PriceId и RegionId
		//но RegionId участвует еще в первичном ключе
		//код вставки записи у хибера
		//не может разрешить эту ситуацию и ломается
		//по этому в мапинге указано что связь с прайс листом вставлять не нужно
		//а PriceId замаплен как отдельное поле
		public virtual uint PriceId { get; set; }

		public virtual Price Price { get; set; }

		public virtual Price LeaderPrice { get; set; }

		//Если есть более одно предложения с одинаковой ценой
		//то в LeaderPrice может быть любой прайс и при отображении прайса лидера
		//мы можем отобразить конкурента хотя должны били отображать самого себя
		public virtual Price ResultLeaderPrice
		{
			get
			{
				if (Leader)
					return Price;
				return LeaderPrice;
			}
		}

		public virtual decimal LeaderCost { get; set; }

		[Style("ResultLeaderPrice.RegionName", "ResultLeaderPrice.Name", Description = "Прайс-лист - лидер")]
		public virtual bool Leader
		{
			get { return LeaderCost == ResultCost || LeaderPrice == Price; }
		}

		[Style(Description = "Препарат запрещен к заказу", Priority = 1)]
		public virtual bool IsForbidden
		{
			get { return BuyingMatrixType == BuyingMatrixStatus.Denied; }
		}

		//подсвечиваем колонки, нужно что бы работал функционал приоритета что бы перекрывался цветом IsForbidden
		[Style("OrderCount", "OrderLine.ResultSum")]
		public virtual bool OrderMark
		{
			get
			{
				return true;
			}
		}

		[Ignore]
		public virtual OrderLine OrderLine
		{
			get { return orderLine; }
			set
			{
				if (orderLine == value)
					return;

				orderLine = value;
				if (OrderLine == null) {
					OrderCount = null;
				}
				else {
					OrderCount = orderLine.Count;
					if (Price.Order == null)
						Price.Order = orderLine.Order;
				}
				OnPropertyChanged();
			}
		}

		[Ignore]
		public virtual decimal? Diff
		{
			get { return _diff; }
		}

		//поле для отображения сгруппированные данных
		//оно здесь потому что PropertyGroupDescription
		//может группировать только по свойству
		//а переделывать механизм группировки не целесообразно
		//используется если снята опция "Поиск по форме выпуска"
		[Ignore]
		public virtual string GroupName { get; set; }

		[Ignore, Style]
		public virtual bool IsGrouped { get; set; }

		public virtual void CalculateDiff(decimal cost)
		{
			if (cost == ResultCost || cost == 0)
				return;

			_diff = Math.Round((ResultCost - cost) / cost, 2);
		}

		[Ignore]
		public virtual uint? OrderCount
		{
			get { return orderCount; }
			set
			{
				orderCount = value;
				OnPropertyChanged();
			}
		}

		//Значение для этого поля загружается асинхронно
		//что бы ui узнал о загрузке надо его оповестить
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
				OnPropertyChanged();
			}
		}

		[Ignore]
		public virtual decimal? PrevOrderAvgCount
		{
			get { return prevOrderAvgCount; }
			set
			{
				prevOrderAvgCount = value;
				OnPropertyChanged();
			}
		}

		[Ignore]
		public virtual bool StatLoaded { get; set; }

		[Style("ProductSynonym", "ProducerSynonym", Description = "Неосновной поставщик", Name = "NotBase")]
		public virtual bool IsNotBase
		{
			get { return Price.NotBase; }
		}

		public virtual List<Message> UpdateOrderLine(Address address, Settings settings,
			Func<string, bool> confirm = null,
			string comment = null,
			bool edit = true)
		{
			var result = new List<Message>();
			if (address == null) {
				OrderCount = null;
				return result;
			}
			var order = Price.Order;
			if (OrderCount.GetValueOrDefault() > 0) {
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
					order.UpdateStat();
				}

				if (edit) {
					result.AddRange(OrderLine.EditValidate());
					result.AddRange(EditValidate(address, settings));
				}
				else {
					result = OrderLine.SaveValidate(confirmCallback: confirm);
				}
				OrderCount = OrderLine.Count;
			}

			if (OrderCount.GetValueOrDefault(0) == 0 && OrderLine != null) {
				if (address.RemoveLine(orderLine)) {
					Price.Order = null;
				}
				OrderLine = null;
			}

			return result;
		}

		private List<Message> EditValidate(Address address, Settings settings)
		{
			var result = new List<Message>();
			if (OrderCount.GetValueOrDefault(0) == 0 || OrderLine == null)
				return result;

			if (Junk)
				result.Add(Message.Warning("Вы заказали препарат с ограниченным сроком годности\r\nили с повреждением вторичной упаковки."));

			if (address.Orders.Where(o => o.Frozen).SelectMany(o => o.Lines).Any(l => l.ProductId == ProductId)) {
				result.Add(Message.Warning("Товар присутствует в замороженных заказах."));
			}
			if (address.YesterdayOrders != null
				&& address.YesterdayOrders.Contains(Tuple.Create(ProductId, OrderCount.GetValueOrDefault()))) {
				result.Add(Message.Warning("Препарат был заказан вчера."));
			}

			if (PrevOrderAvgCount != null && OrderCount > PrevOrderAvgCount * settings.OverCountWarningFactor) {
				result.Add(Message.Warning("Превышение среднего заказа!"));
			}

			if (PrevOrderAvgCost != null && Cost > PrevOrderAvgCost * (1 + settings.OverCostWarningPercent / 100)) {
				result.Add(Message.Warning("Превышение средней цены!"));
			}

			if (BuyingMatrixType == BuyingMatrixStatus.Denied) {
				result.Clear();
				OrderLine.Count = 0;
				result.Add(Message.Warning("Препарат запрещен к заказу."));
			}

			return result;
		}

		public virtual List<Message> SaveOrderLine(Address address, Settings settings, Func<string, bool> confirm = null, string comment = null)
		{
			return UpdateOrderLine(address, settings, confirm, comment, false);
		}

		[Ignore]
		public virtual uint Value
		{
			get { return OrderCount.GetValueOrDefault(); }
			set { OrderCount = value; }
		}

		/// <summary>
		/// Результирующая цена, цена поставщика + корректировка назначенная аптекой
		/// </summary>
		public virtual decimal ResultCost
		{
			get {
				return GetResultCost(Price);
			}
		}

		public override decimal GetResultCost()
		{
			return ResultCost;
		}

		//перегрузка Equals и GetHashCode
		//нужна что бы DataGrid сохранял выделенную позицию после обновления данных
		public override bool Equals(object obj)
		{
			var that = obj as Offer;
			if (that == null)
				return false;

			return Id.Equals(that.Id);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public static IQueryable<Offer> Orderable(IQueryable<Offer> query)
		{
			return query.Where(o => o.BuyingMatrixType != BuyingMatrixStatus.Denied && !o.Price.IsOrderDisabled);
		}
	}
}