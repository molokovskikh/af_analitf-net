using System;
using System.Collections.Generic;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class SentOrderLine : BaseOffer, IOrderLine
	{
		private bool _isUnmatchedByWaybill;

		public SentOrderLine()
		{
		}

		public SentOrderLine(SentOrder order, OrderLine orderLine)
			: base(orderLine)
		{
			Order = order;
			ServerOrderId = order.ServerId;
			Count = orderLine.Count;
			Comment = orderLine.Comment;
			ResultCost = orderLine.ResultCost;
			ServerId = orderLine.ExportId;
		}

		public virtual uint Id { get; set; }

		public virtual uint? ServerId { get; set; }

		public virtual ulong? ServerOrderId { get; set; }

		public virtual uint Count { get; set; }

		public virtual string Comment { get; set; }

		public virtual decimal ResultCost { get; set; }

		public virtual decimal MixedCost => HideCost ? ResultCost : Cost;

		public virtual decimal Sum => Count * Cost;

		public virtual decimal MixedSum => Count * MixedCost;

		public virtual SentOrder Order { get; set; }

		//заглушка, для поля редактирования на форме детализации заказа
		//хоть поле которое содержит ошибку скрыто для отправленных заказов
		//но биндинг все равно будет ругаться если поля не будет
		[Ignore]
		public virtual string LongSendError { get; set; }

		//загружается асинхронно
		[Ignore, Style(Description = "Несоответствие в накладной")]
		public virtual bool IsUnmatchedByWaybill
		{
			get { return _isUnmatchedByWaybill; }
			set
			{
				if (_isUnmatchedByWaybill == value)
					return;
				_isUnmatchedByWaybill = value;
				OnPropertyChanged();
			}
		}

		[Style("Order.AddressName")]
		public virtual bool IsCurrentAddress => Order.IsCurrentAddress;

		public virtual void Configure(User user, Dictionary<uint, WaybillLine[]> matchedWaybillLines)
		{
			Configure(user);
			Configure(matchedWaybillLines);
		}

		public virtual void Configure(Dictionary<uint, WaybillLine[]> matchedWaybillLines)
		{
			if (ServerId == null)
				return;
			var lines = matchedWaybillLines.GetValueOrDefault(ServerId.Value);
			if (lines == null || lines.Length == 0) {
				IsUnmatchedByWaybill = true;
				return;
			}
			IsUnmatchedByWaybill = lines[0].Quantity < Count
				|| Math.Abs((double)(Cost - lines[0].SupplierCost.GetValueOrDefault())) > 0.02;
		}

		public override decimal GetResultCost()
		{
			return ResultCost;
		}
	}
}