using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NHibernate;

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
				if (_status != value)
				{
					_status = value;
					OnPropertyChanged();
				}
			}
		}

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual DateTime? CloseDate { get; set; }
		public virtual decimal? SupplySumWithoutNds { get; set; }
		public virtual decimal? SupplySum { get; set; }
		public virtual decimal? RetailSum { get; set; }
		public virtual decimal LinesCount { get; set; }

		public virtual IList<InventoryDocLine> Lines { get; set; }

		public virtual void Close()
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Closed;
			// с поставки на склад
			foreach (var line in Lines)
				line.Stock.Incoming(line.Quantity);
		}

		public virtual void ReOpen()
		{
			CloseDate = null;
			Status = DocStatus.Opened;
			// со склада в поставку
			foreach (var line in Lines)
				line.Stock.CancelIncoming(line.Quantity);
		}

		public virtual void BeforeDelete(ISession session)
		{
			// с поставки наружу
			foreach (var line in Lines)
				session.Save(line.Stock.CancelInventoryDoc(line.Quantity));
		}

		public virtual void UpdateStat()
		{
			LinesCount = Lines.Count;
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplySumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplySum = Lines.Sum(x => x.Quantity*x.SupplierCost);
		}
	}

	public class InventoryDocLine : BaseStock
	{
		public InventoryDocLine()
		{
		}

		public InventoryDocLine(Stock stock, decimal quantity, ISession session)
		{
			Stock.Copy(stock, this);
			Id = 0;
			Stock = stock;
			Quantity = quantity;
			session.Save(Stock.InventoryDoc(quantity));
		}

		public virtual uint Id { get; set; }

		public virtual decimal SupplierSumWithoutNds => Quantity * SupplierCostWithoutNds.GetValueOrDefault();

		public virtual decimal SupplierSum => Quantity * SupplierCost.GetValueOrDefault();

		public virtual decimal RetailSum => Quantity * RetailCost.GetValueOrDefault();

		public virtual decimal Quantity { get; set; }

		public virtual DateTime DocumentDate { get; set; }

		public virtual string WaybillNumber { get; set; }

		public virtual Stock Stock { get; set; }

		public virtual void UpdateQuantity(decimal quantity, ISession session)
		{
			// с поставки наружу
			session.Save(Stock.CancelInventoryDoc(Quantity));
			// снаружи в поставку
			session.Save(Stock.InventoryDoc(quantity));
			Quantity = quantity;
		}
	}
}
