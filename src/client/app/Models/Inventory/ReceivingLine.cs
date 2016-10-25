using System;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReceivingLine : BaseStock
	{
		public ReceivingLine()
		{
		}

		public ReceivingLine(WaybillLine line)
		{
		}

		public virtual uint Id { get; set; }

		public virtual string CountryCode { get; set; }
		public virtual string Country { get; set; }

		public virtual string Period { get; set; }
		public virtual DateTime? Exp { get; set; }

		public virtual string Unit { get; set; }
		public virtual int? Nds { get; set; }

		public virtual decimal Quantity { get; set; }
		public virtual decimal Sum => Quantity * SupplierCost.GetValueOrDefault();
		public virtual uint ReceivingOrderId { get; set; }

		public virtual void CopyToStock(Stock stock)
		{
			Stock.Copy(this, stock);
		}

		public virtual void CopyFromStock(Stock stock)
		{
			Stock.Copy(stock, this);
		}
	}
}