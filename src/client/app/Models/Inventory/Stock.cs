using System;
using System.Linq;
using System.Collections.Generic;
using AnalitF.Net.Client.Helpers;
using System.ComponentModel;
using NHibernate;
using NHibernate.Linq;

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

	public class BaseStock : BaseNotify
	{
		public virtual string Barcode { get; set; }
		public virtual string Product { get; set; }
		public virtual uint? ProductId { get; set; }
		public virtual uint? CatalogId { get; set; }
		public virtual string Producer { get; set; }
		public virtual uint? ProducerId { get; set; }
		public virtual string SerialNumber { get; set; }
		public virtual string Certificates { get; set; }
		public virtual decimal? ProducerCost { get; set; }
		public virtual decimal? RegistryCost { get; set; }
		public virtual decimal? RetailCost { get; set; }
		public virtual decimal? RetailMarkup { get; set; }

		public virtual decimal? SupplierCost { get; set; }
		public virtual decimal? SupplierCostWithoutNds { get; set; }
		public virtual decimal? SupplierPriceMarkup { get; set; }

		public virtual decimal? ExciseTax { get; set; }
		public virtual string BillOfEntryNumber { get; set; }
		public virtual bool? VitallyImportant { get; set; }

		public virtual decimal SupplyQuantity { get; set; }
	}

	public class Stock : BaseStock
	{
		public Stock()
		{
		}

		public Stock(ReceivingOrder order, ReceivingLine line)
		{
			WaybillId = order.WaybillId;
			Status = StockStatus.Available;
			Address = order.Address;
			line.CopyToStock(this);
		}

		public virtual uint Id { get; set; }

		public virtual ulong? ServerId { get; set; }
		public virtual int? ServerVersion { get; set; }

		public virtual Address Address { get; set; }
		public virtual StockStatus Status { get; set; }

		public virtual uint? ReceivingOrderId { get; set; }
		public virtual uint? WaybillId { get; set; }

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
		public virtual decimal Quantity { get; set; }
		public virtual decimal ReservedQuantity { get; set; }

		public virtual int? Nds { get; set; }
		public virtual decimal? NdsAmount { get; set; }
		public virtual double NdsPers { get; set; }
		public virtual double NpPers { get; set; }
		public virtual decimal Excise { get; set; }

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual decimal? LowCost { get; set; }
		public virtual decimal? LowMarkup
		{
			get
			{
				if (RetailCost != 0 && LowCost != null)
					return Math.Round(((LowCost - SupplierCost) * 100 / SupplierCost).Value, 2);
				return null;
			}
		}

		public virtual decimal? OptCost { get; set; }
		public virtual decimal? OptMarkup
		{
			get
			{
				if (SupplierCost != 0 && OptCost != null)
					return Math.Round((((OptCost - SupplierCost) * 100) / SupplierCost).Value, 2);
				return null;
			}
		}

		public virtual decimal SupplySum => SupplyQuantity * SupplierCost.GetValueOrDefault();
		public virtual decimal SupplySumWithoutNds => SupplyQuantity * SupplierCostWithoutNds.GetValueOrDefault();
		public virtual decimal? RetailSum => Quantity * RetailCost;
		public virtual string Vmn { get; set; }
		public virtual string Gtd { get; set; }
		public virtual DateTime? Exp { get; set; }
		public virtual string Period { get; set; }
		public virtual string DocumentDate { get; set; }
		public virtual string WaybillNumber { get; set; }

		public virtual RejectStatus RejectStatus { get; set; }

		public virtual string RejectStatusName => DescriptionHelper.GetDescription(RejectStatus);

		public static IQueryable<Stock> AvailableStocks(IStatelessSession session, Address address = null)
		{
			var query = session.Query<Stock>().Where(x => x.Quantity > 0 && x.Status == StockStatus.Available);
			if (address != null)
				query = query.Where(x => x.Address == address);
			return query;
		}

		public virtual StockAction ApplyReserved(decimal quantity)
		{
			ReservedQuantity -= quantity;
			return new StockAction(ActionType.Sale, this, quantity);
		}

		public virtual void Release(decimal quantity)
		{
			ReservedQuantity -= quantity;
			Quantity += quantity;
		}

		public virtual void Reserve(decimal quantity)
		{
			Quantity -= quantity;
			ReservedQuantity += quantity;
		}
	}
}