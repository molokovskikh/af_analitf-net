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
			Product = line.Product;
			ProductId = line.ProductId;
			Producer = line.Producer;
			ProducerId = line.ProductId;
			Country = line.Country;
			CountryCode = line.CountryCode;
			Period = line.Period;
			Exp = line.Exp;
			SerialNumber = line.SerialNumber;
			Certificates = line.Certificates;
			Unit = line.Unit;
			ExciseTax = line.ExciseTax;
			BillOfEntryNumber = line.BillOfEntryNumber;
			VitallyImportant = line.VitallyImportant;
			ProducerCost = line.ProducerCost;
			RegistryCost = line.RegistryCost;
			SupplierPriceMarkup = line.SupplierPriceMarkup;
			SupplierCostWithoutNds = line.SupplierCostWithoutNds;
			Nds = line.Nds;
			Barcode = line.EAN13;

			Quantity = line.QuantityToReceive;
			SupplyQuantity = line.Quantity.GetValueOrDefault();
			SupplierCost = line.SupplierCost.GetValueOrDefault();
			RetailCost = line.RetailCost.GetValueOrDefault();
			RetailMarkup = line.RetailMarkup;
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
			Copy(this, stock);
		}

		public virtual void CopyFromStock(Stock stock)
		{
			Copy(stock, this);
		}

		public static void Copy(object srcItem, object dstItem)
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