using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalitF.Net.Client.Models
{
	public class Waybill
	{
		public Waybill()
		{
			Lines = new List<WaybillLine>();
		}

		public virtual uint Id { get; set; }
		public virtual string ProviderDocumentId { get; set; }
		public virtual DateTime DocumentDate { get; set; }
		public virtual DateTime WriteTime { get; set; }
		public virtual Address Address { get; set; }
		public virtual Supplier Supplier { get; set; }

		public virtual decimal Sum { get; set; }
		public virtual decimal RetailSum { get; set; }
		public virtual decimal TaxSum { get; set; }

		public virtual IList<WaybillLine> Lines { get; set; }

		public virtual decimal SumWithoutTax
		{
			get { return Sum - TaxSum; }
		}

		public virtual decimal Markup
		{
			get { return Sum > 0 ? Math.Round(MarkupSum / Sum * 100, 2) : 0; }
		}

		public virtual decimal MarkupSum
		{
			get { return RetailSum - Sum; }
		}

		public virtual string Type
		{
			get { return "Накладная"; }
		}

		public virtual void Calculate(Settings settings, IEnumerable<MarkupConfig> markups, bool round)
		{
			foreach (var waybillLine in Lines)
				waybillLine.Calculate(settings, markups, round);

			Sum = Lines.Sum(l => l.SupplierCost * l.Quantity).GetValueOrDefault();
			RetailSum = Lines.Sum(l => l.RetailCost * l.Quantity).GetValueOrDefault();
		}
	}
}