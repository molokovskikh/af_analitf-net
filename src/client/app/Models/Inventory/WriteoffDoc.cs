using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using AnalitF.Net.Client.Config.NHibernate;

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

		[Ignore]
		[Style(Description = "Непроведенный документ")]
		public virtual bool IsNotPosted => Status == DocStatus.NotPosted;

		public virtual IList<WriteoffLine> Lines { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				if ((columnName == nameof(Reason)) && (Reason == null))
					return "Поле 'Причина' должно быть заполнено";
				if ((columnName == nameof(Address)) && (Address == null))
					return "Поле 'Адрес' должно быть заполнено";
				if (columnName == nameof(Lines) && !Lines.Any())
					return "Документ не может быть пустым";
				return null;
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] {nameof(Address), nameof(Reason), nameof(Lines) };

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
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
			WaybillLineId = stock.WaybillLineId;
			Stock = stock;
			Quantity = quantity;
			Stock.Reserve(Quantity);
		}

		public virtual uint Id { get; set; }

		public virtual uint? WaybillLineId { get; set; }

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