using System;
using AnalitF.Net.Client.Helpers;
using System.ComponentModel;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum StockStatus
	{
		[Description("Доступен")] Available,
		[Description("В пути")] InTransit,
	}

	public enum RejectStatus
	{
		[Description("Неизвестно")]
		Unknown,
		[Description("Возможно")]
		Perhaps,
		[Description("Брак")]
		Defective,
		[Description("Нет")]
		NotDefective,
	}

	public class BaseStock
	{
		public virtual string Barcode { get; set; }
		public virtual string Product { get; set; }
		public virtual uint? ProductId { get; set; }
		public virtual string Producer { get; set; }
		public virtual uint? ProducerId { get; set; }
	}

	public class Stock : BaseStock
	{
		public virtual uint Id { get; set; }

		public virtual StockStatus Status { get; set; }

		public virtual uint? ReceivingOrderId { get; set; }

		public virtual string AnalogCode { get; set; }
		public virtual string ProducerBarcode { get; set; }
		public virtual string AltBarcode { get; set; }
		public virtual string AnalogGroup { get; set; }
		public virtual string Country { get; set; }
		public virtual string Unit { get; set; }

		public virtual string ProductKind { get; set; }

		public virtual string FarmGroup { get; set; }
		public virtual string Mnn { get; set; }
		public virtual string Brand { get; set; }
		public virtual string UserCategory { get; set; }
		public virtual string Category { get; set; }
		public virtual string RegionCert { get; set; }
		public virtual string Certificate { get; set; }
		public virtual decimal Count { get; set; }
		public virtual decimal Cost { get; set; }
		public virtual decimal RetailCost { get; set; }
		public virtual decimal ProducerCost { get; set; }
		public virtual decimal Nds { get; set; }
		public virtual double NdsPers { get; set; }
		public virtual double NpPers { get; set; }
		public virtual decimal Excise { get; set; }
		public virtual decimal CostWithNds => Cost + Nds + Excise;

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual decimal RetailMarkup
		{
			get
			{
				if (Cost != 0)
					return Math.Round(((RetailCost - Cost) * 100) / Cost, 2);
				return 0;
			}
		}

		public virtual decimal? LowCost { get; set; }
		public virtual decimal? LowMarkup
		{
			get
			{
				if (Cost != 0 && LowCost != null)
					return Math.Round((((LowCost - Cost) * 100) / Cost).Value, 2);
				return null;
			}
		}

		public virtual decimal? OptCost { get; set; }
		public virtual decimal? OptMarkup
		{
			get
			{
				if (Cost != 0 && OptCost != null)
					return Math.Round((((OptCost - Cost) * 100) / Cost).Value, 2);
				return null;
			}
		}

		public virtual string Seria { get; set; }

		public virtual decimal Sum => Count * Cost;
		public virtual decimal SumWithNds { get; set; }
		public virtual decimal RetailSum => Count * RetailCost;
		public virtual uint CountDelivery { get; set; }
		public virtual string Vmn { get; set; }
		public virtual string Gtd { get; set; }
		public virtual string Period { get; set; }
		public virtual string DocumentDate { get; set; }
		public virtual string WaybillNumber { get; set; }

		public virtual RejectStatus RejectStatus { get; set; }

		public virtual string RejectStatusName => DescriptionHelper.GetDescription(RejectStatus);

	}
}