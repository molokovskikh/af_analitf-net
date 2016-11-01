using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class WriteoffDoc : BaseNotify, IDataErrorInfo2
	{
		private DocStatus _status;

		public WriteoffDoc()
		{
			Lines = new List<WriteoffLine>();
		}

		public WriteoffDoc(Address address)
			: this()
		{
			Date = DateTime.Now;
			Address = address;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual Address Address { get; set; }
		public virtual WriteoffReason Reason { get; set; }

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

		public virtual DateTime? CloseDate { get; set; }
		public virtual decimal? SupplySumWithoutNds { get; set; }
		public virtual decimal? SupplySum { get; set; }
		public virtual decimal? RetailSum { get; set; }
		public virtual decimal LinesCount { get; set; }

		public virtual IList<WriteoffLine> Lines { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				if ((columnName == nameof(Reason)) && (Reason == null)) return "Поле 'Причина' должно быть заполнено";
				if ((columnName == nameof(Address)) && (Address == null)) return "Поле 'Адрес' должно быть заполнено";
				return null;
			}
		}

		public virtual string Error { get; }
		public virtual string[] FieldsForValidate => new[] {nameof(Address), nameof(Reason)};

		public virtual void Close(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Closed;
			foreach (var line in Lines)
				session.Save(line.Stock.ApplyReserved(line.Quantity));
		}

		public virtual void UpdateStat()
		{
			LinesCount = Lines.Count;
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplySumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplySum = Lines.Sum(x => x.Quantity*x.SupplierCost);
		}
	}


	public class WriteoffLine : BaseStock
	{
		public WriteoffLine()
		{
		}

		public WriteoffLine(Stock stock, decimal quantity)
		{
			Stock.Copy(stock, this);
			Id = 0;
			Stock = stock;
			Quantity = quantity;
			Stock.Reserve(Quantity);
		}

		public virtual uint Id { get; set; }

		public virtual DateTime? Exp { get; set; }

		public virtual string Period { get; set; }

		public virtual decimal? SupplierSumWithoutNds => SupplierCostWithoutNds*Quantity;

		public virtual decimal? SupplierSum => SupplierCost * Quantity;

		public virtual decimal? RetailSum => RetailCost*Quantity;

		public virtual decimal Quantity { get; set; }

		public virtual Stock Stock { get; set; }

		public virtual void UpdateQuantity(decimal quantity)
		{
			Stock.Release(Quantity);
			Stock.Reserve(quantity);
			Quantity = quantity;
		}
	}
}