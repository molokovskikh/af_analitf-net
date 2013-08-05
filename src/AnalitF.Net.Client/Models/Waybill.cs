using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Navigation;
using AnalitF.Net.Client.Config.Initializers;

namespace AnalitF.Net.Client.Models
{
	public class Contractor
	{
		public virtual string Name { get; set; }
		public virtual string Address { get; set; }
		public virtual string Inn { get; set; }
		public virtual string Kpp { get; set; }
	}

	public class Waybill
	{
		private bool _vitallyImportant;
		private Settings _settings = new Settings();

		public Waybill()
		{
			Lines = new List<WaybillLine>();
			RoundTo1 = true;
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

		public virtual Contractor Seller { get; set; }
		public virtual Contractor Buyer { get; set; }
		public virtual string ShipperNameAndAddress { get; set; }
		public virtual string ConsigneeNameAndAddress { get; set; }

		public virtual IList<WaybillLine> Lines { get; set; }

		[Ignore]
		public virtual bool RoundTo1 { get; set; }

		[Ignore]
		public virtual bool VitallyImportant
		{
			get { return _vitallyImportant; }
			set
			{
				if (!CanBeVitallyImportant)
					return;
				if (_vitallyImportant == value)
					return;

				_vitallyImportant = value;
				Calculate(_settings);
			}
		}

		[Ignore]
		public virtual bool CanBeVitallyImportant
		{
			get
			{
				return Lines.All(l => l.VitallyImportant == null);
			}
		}

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

		public virtual string SupplierName
		{
			get { return Supplier == null ? null : Supplier.FullName; }
		}

		public virtual void Calculate(Settings settings)
		{
			_settings = settings;
			foreach (var waybillLine in Lines)
				waybillLine.Calculate(_settings, _settings.Markups);

			Sum = Lines.Sum(l => l.SupplierCost * l.Quantity).GetValueOrDefault();
			CalculateRetailSum();
		}

		public virtual void CalculateRetailSum()
		{
			RetailSum = Lines.Sum(l => l.RetailCost * l.Quantity).GetValueOrDefault();
		}
	}
}