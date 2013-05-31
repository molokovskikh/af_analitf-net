using System;
using System.Collections.Generic;
using AnalitF.Net.Client.Config.Initializers;

namespace AnalitF.Net.Client.Models
{
	public class WaybillLine
	{
		public WaybillLine()
		{
		}

		public WaybillLine(Waybill waybill)
		{
			Waybill = waybill;
		}

		public virtual uint Id { get; set; }
		public virtual Waybill Waybill { get; set; }
		public virtual string Product { get; set; }
		public virtual string Producer { get; set; }
		public virtual string Country { get; set; }
		public virtual bool Print { get; set; }
		public virtual string Period { get; set; }
		public virtual string SerialNumber { get; set; }
		public virtual string Certificates { get; set; }
		public virtual bool LoadCertificate { get; set; }
		public virtual string CertificateNumber { get; set; }

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

		public virtual decimal? Amount { get; set; }

		public virtual decimal? RealMarkup { get; set; }

		public virtual int? Quantity { get; set; }

		[Ignore]
		public virtual decimal? MaxRetailMarkup { get; set; }

		[Ignore]
		public virtual decimal? RetailMarkup { get; set; }

		[Ignore]
		public virtual decimal? RealRetailMarkup { get; set; }

		[Ignore]
		public virtual decimal? RetailCost { get; set; }

		[Ignore]
		public virtual decimal? RetailSum
		{
			get { return Quantity * RetailCost; }
		}

		public virtual decimal? AmountExcludeTax
		{
			get { return Amount - NdsAmount; }
		}

		public virtual decimal? ProducerCostWithTax
		{
			get { return ProducerCost * (1 + (decimal?) Nds / 100); }
		}

		public virtual void Calculate(Settings settings, IEnumerable<MarkupConfig> markups, bool round)
		{
			if (SupplierCost == null)
				return;
			var vitallyImportant = VitallyImportant.GetValueOrDefault();
			var sourceCost = (vitallyImportant && settings.LookupMarkByProducerCost ? ProducerCost : SupplierCostWithoutNds).GetValueOrDefault();
			if (sourceCost == 0)
				return;
			var markup = MarkupConfig.Calculate(markups, vitallyImportant ? MarkupType.VitallyImportant : MarkupType.Over, sourceCost);
			if (markup == null)
				return;

			MaxRetailMarkup = markup.MaxMarkup;
			RetailCost = CalculateRetailCost(settings, markup.Markup, round);
			RetailMarkup = Math.Round(((RetailCost - SupplierCost) / SupplierCost * 100).GetValueOrDefault(), 2);
		}

		private decimal? CalculateRetailCost(Settings settings, decimal markup, bool rountTo1)
		{
			var vitallyImportant = VitallyImportant.GetValueOrDefault();
			decimal value;
			if (vitallyImportant) {
				value = ((SupplierCostWithoutNds + ProducerCost * (markup / 100)) * (100 + Nds) / 100).GetValueOrDefault();
			}
			else {
				value = (SupplierCost  + SupplierCostWithoutNds * (100 + Nds) / 100 * markup / 100).GetValueOrDefault();
			}
			value = Math.Round(value, 2);
			if (rountTo1)
				return ((int)(value * 10))/10m;
			return value;
		}

		public virtual string SupplierName
		{
			get { return Waybill.Supplier == null ? null : Waybill.Supplier.Name; }
		}
	}
}