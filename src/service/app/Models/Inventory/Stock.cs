using System;
using Common.Models;
using NHibernate;

namespace AnalitF.Net.Service.Models.Inventory
{
	public enum StockStatus
	{
		Available,
		InTransit
	}

	public class Stock
	{
		public Stock(User user, Stock source, decimal quantity, uint clientId, decimal retailCost, decimal retailMarkup)
		{
			Address = source.Address;
			ProductId = source.ProductId;
			CatalogId = source.CatalogId;
			Product = source.Product;
			ProducerId = source.ProducerId;
			Producer = source.Producer;
			Country = source.Country;
			CountryCode = source.CountryCode;
			Period = source.Period;
			Exp = source.Exp;
			SerialNumber = source.SerialNumber;
			Certificates = source.Certificates;
			Unit = source.Unit;
			ExciseTax = source.ExciseTax;
			BillOfEntryNumber = source.BillOfEntryNumber;
			VitallyImportant = source.VitallyImportant;
			ProducerCost = source.ProducerCost;
			RegistryCost = source.RegistryCost;
			SupplierPriceMarkup = source.SupplierPriceMarkup;
			SupplierCostWithoutNds = source.SupplierCostWithoutNds;
			SupplierCost = source.SupplierCost;
			Nds = source.Nds;
			NdsAmount = source.NdsAmount;
			Barcode = source.Barcode;
			SupplyQuantity = source.SupplyQuantity;

			Status = StockStatus.Available;
			CreatedByUser = user;
			Quantity = Math.Abs(quantity);
			ClientPrimaryKey = clientId;
			RetailCost = retailCost;
			RetailMarkup = retailMarkup;

			WaybillNumber = source.WaybillNumber;
			SupplierId = source.SupplierId;
			SupplierFullName = source.SupplierFullName;
		}

	public Stock()
		{
		}

		public virtual ulong Id { get; set; }
		public virtual int Version { get; set; }
		public virtual uint? ClientPrimaryKey { get; set; }
		public virtual DateTime Timestamp { get; set; }

		public virtual StockStatus Status { get; set; }

		public virtual uint? WaybillLineId { get; set; }
		public virtual Address Address { get; set; }
		public virtual User CreatedByUser { get; set; }

		public virtual uint? ProductId { get; set; }
		public virtual uint? CatalogId { get; set; }
		public virtual string Product { get; set; }
		public virtual uint? ProducerId { get; set; }
		public virtual string Producer { get; set; }

		public virtual string CountryCode { get; set; }
		public virtual string Country { get; set; }

		public virtual string Period { get; set; }
		public virtual DateTime? Exp { get; set; }
		public virtual string SerialNumber { get; set; }
		public virtual string Certificates { get; set; }

		public virtual string Unit { get; set; }
		public virtual decimal? ExciseTax { get; set; }
		public virtual string BillOfEntryNumber { get; set; }

		public virtual bool? VitallyImportant { get; set; }

		public virtual decimal? ProducerCost { get; set; }
		public virtual decimal? RegistryCost { get; set; }

		public virtual decimal? SupplierPriceMarkup { get; set; }
		public virtual decimal? SupplierCostWithoutNds { get; set; }
		public virtual decimal? SupplierCost { get; set; }

		public virtual int? Nds { get; set; }
		public virtual decimal? NdsAmount { get; set; }

		public virtual string Barcode { get; set; }

		public virtual decimal Quantity { get; set; }
		public virtual decimal SupplyQuantity { get; set; }
		//розничная цена null для позиций которые в пути
		public virtual decimal? RetailCost { get; set; }
		public virtual decimal? RetailMarkup { get; set; }

		public virtual string WaybillNumber { get; set; }
		public virtual uint? SupplierId { get; set; }
		public virtual string SupplierFullName { get; set; }

		public static void CreateInTransitStocks(ISession session, User user)
		{
			session.CreateSQLQuery(@"
insert into Inventory.Stocks(WaybillLineId,
	Status,
	AddressId,
	ProductId,
	CatalogId,
	Product,
	ProducerId,
	Producer,
	Country,
	ProducerCost,
	RegistryCost,
	SupplierPriceMarkup,
	SupplierCostWithoutNds,
	SupplierCost,
	Quantity,
	SupplyQuantity,
	Nds,
	SerialNumber,
	NdsAmount,
	Unit,
	BillOfEntryNumber,
	ExciseTax,
	VitallyImportant,
	Period,
	Exp,
	Certificates,
	Barcode,
	CountryCode,
	WaybillNumber,
	SupplierId,
	SupplierFullName
)
select db.Id,
	:status,
	dh.AddressId,
	db.ProductId,
	p.CatalogId,
	IFNULL(c.Name, db.Product),
	db.ProducerId,
	db.Producer,
	db.Country,
	db.ProducerCost,
	db.RegistryCost,
	db.SupplierPriceMarkup,
	db.SupplierCostWithoutNds,
	db.SupplierCost,
	db.Quantity,
	db.Quantity,-- SupplyQuantity
	db.Nds,
	db.SerialNumber,
	db.NdsAmount,
	db.Unit,
	db.BillOfEntryNumber,
	db.ExciseTax,
	db.VitallyImportant,
	db.Period,
	str_to_date(db.Period, '%d.%m.%Y') as Exp,
	db.Certificates,
	db.EAN13 as Barcode,
	db.CountryCode,
	dh.ProviderDocumentId,
	dh.FirmCode,
	sp.FullName
from Customers.UserAddresses ua
	join Customers.Addresses a on a.Id = ua.AddressId
		join Documents.DocumentHeaders dh on dh.Addressid = a.Id
			join Documents.DocumentBodies db on db.DocumentId = dh.Id
				left join Catalogs.Products p on p.Id = db.ProductId
				left join Catalogs.Catalog c on c.Id = p.CatalogId
				left join Customers.Suppliers sp on sp.Id = dh.FirmCode
				left join Inventory.Stocks s on s.WaybillLineId = db.Id
where ua.UserId = :userId
	and a.Enabled = 1
	and s.Id is null
	and dh.WriteTime > curdate() - interval 10 day")
				.SetParameter("userId", user.Id)
				.SetParameter("status", StockStatus.InTransit)
				.ExecuteUpdate();
		}
	}
}