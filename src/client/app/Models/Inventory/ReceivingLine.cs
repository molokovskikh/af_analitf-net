using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum ReceivingLineStatus
	{
		[Description("Новый")] New,
		[Description("В процессе")] InProgress,
		[Description("Закрыт")] Closed
	}

	public class ReceivingLine
	{
		public virtual uint Id { get; set; }
		public virtual string Product { get; set; }
		public virtual string Producer { get; set; }
		public virtual decimal RetailCost { get; set; }
		public virtual ReceivingLineStatus Status { get; set; }
		public virtual decimal Cost { get; set; }
		public virtual decimal Count { get; set; }
		public virtual decimal Sum => Count * Cost;
		public virtual uint ReceivingOrderId { get; set; }
		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		[Ignore]
		public virtual List<ReceivingDetail> Details { get; set; }

		public virtual void UpdateStatus()
		{
			var receivedCount = Details.Where(x => x.Status == DetailStatus.Received).Sum(x => x.Count);
			if (Count <= receivedCount)
				Status = ReceivingLineStatus.Closed;
			else if (Details.Count == 0)
				Status = ReceivingLineStatus.New;
			else
				Status = ReceivingLineStatus.InProgress;
		}

		public virtual void CopyToStock(Stock stock)
		{
			Copy(this, stock);
		}

		public virtual void CopyFromStock(Stock stock)
		{
			Copy(stock, this);
		}

		private static void Copy(object srcItem, object dstItem)
		{
			var srcProps = srcItem.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite);
			var dstProps = dstItem.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite).ToDictionary(x => x.Name);
			foreach (var srcProp in srcProps) {
				var dstProp = dstProps.GetValueOrDefault(srcProp.Name);
				dstProp?.SetValue(dstItem, srcProp.GetValue(srcItem, null), null);
			}
		}
	}
}