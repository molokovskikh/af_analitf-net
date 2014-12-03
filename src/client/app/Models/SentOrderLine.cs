using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Shapes;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NHibernate.Mapping;

namespace AnalitF.Net.Client.Models
{
	public class SentOrderLine : BaseOffer, IOrderLine
	{
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

		public virtual decimal MixedCost
		{
			get
			{
				return HideCost ? ResultCost : Cost;
			}
		}

		public virtual decimal Sum
		{
			get { return Count * Cost; }
		}

		public virtual decimal MixedSum
		{
			get { return Count * MixedCost; }
		}

		public virtual SentOrder Order { get; set; }

		//заглушка, для поля редактирования на форме детализации заказа
		//хоть поле которое содержит ошибку скрыто для отправленных заказов
		//но биндинг все равно будет ругаться если поля не будет
		[Ignore]
		public virtual string LongSendError { get; set; }

		[Ignore, Style(Description = "Несоответствие в накладной")]
		public virtual bool IsUnmatchedByWaybill { get; set; }

		public virtual void Configure(User user, Dictionary<uint, WaybillLine[]> matchedWaybillLines)
		{
			Configure(user);

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