using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum DocStatus
	{
		[Description("Открыт")] Opened,
		[Description("Закрыт")] Closed
	}

	public class InventoryDoc : BaseNotify
	{
		private DocStatus _status;

		public InventoryDoc()
		{
			Lines = new List<InventoryDocLine>();
		}

		public InventoryDoc(Address address)
			: this()
		{
			Date = DateTime.Now;
			Address = address;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual Address Address { get; set; }

		public virtual DocStatus Status
		{
			get { return _status; }
			set
			{
				if (_status != value) {
					_status = value;
					OnPropertyChanged();
				}
			}
		}

		public virtual DateTime? CloseDate { get; set; }
		public virtual decimal? SupplySumWithoutNds { get; set; }
		public virtual decimal? SupplySum { get; set; }
		public virtual decimal? RetailSum { get; set; }
		public virtual decimal LinesCount { get; set; }

		public virtual IList<InventoryDocLine> Lines { get; set; }

		public virtual void Close(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Closed;
		}

		public virtual void UpdateStat()
		{
			LinesCount = Lines.Count;
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplySumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplySum = Lines.Sum(x => x.Quantity * x.SupplierCost);
		}
	}

	public class InventoryDocLine : BaseStock
	{
		public InventoryDocLine()
		{
		}

		public InventoryDocLine(Catalog catalog)
		{
			Product = catalog.FullName;
			CatalogId = catalog.Id;
		}

		public virtual uint Id { get; set; }

		public virtual DateTime? Period { get; set; }

		public virtual decimal? SupplierSumWithoutNds => SupplierCostWithoutNds * Quantity;

		public virtual decimal? RetailSum => RetailCost * Quantity;

		public virtual decimal? Quantity { get; set; }

		public virtual InventoryDocLine Copy()
		{
			return (InventoryDocLine)MemberwiseClone();
		}

		public virtual void CopyFrom(InventoryDocLine line)
		{
			ReceivingLine.Copy(line, this);
			typeof(InventoryDocLine).GetProperties().Each(x => OnPropertyChanged(x.Name));
		}
	}
}