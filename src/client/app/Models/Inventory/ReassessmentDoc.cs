using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReassessmentDoc : BaseNotify, IDataErrorInfo2
	{
		private DocStatus _status;

		public ReassessmentDoc()
		{
			Lines = new List<ReassessmentLine>();
		}

		public ReassessmentDoc(Address address)
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

		public virtual IList<ReassessmentLine> Lines { get; set; }

		public virtual void Close(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Closed;
			foreach (var line in Lines) {
				session.Save(line.SrcStock.ApplyReserved(line.Quantity));

				line.DstStock.Quantity += line.Quantity;
				session.Save(line.DstStock.ApplyReserved(line.Quantity));
			}
		}

		public virtual void UpdateStat()
		{
			LinesCount = Lines.Count;
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplySumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplySum = Lines.Sum(x => x.Quantity * x.SupplierCost);
		}

			public virtual string this[string columnName]
			{
				get
				{
					if (columnName == nameof(Address) && Address == null) {
						return "Поле 'Адрес' должно быть заполнено";
					}
					return null;
				}
			}

			public virtual string Error { get; }
			public virtual string[] FieldsForValidate => new [] { nameof(Address) };

		public virtual void DeleteLine(ReassessmentLine line)
		{
			line.SrcStock.Release(line.Quantity);
			line.DstStock.ReservedQuantity -= line.Quantity;
			Lines.Remove(line);
		}
	}


	public class ReassessmentLine : BaseStock
	{
		public ReassessmentLine()
		{
		}

		public ReassessmentLine(Stock srcStock, Stock dstStock)
		{
			ReceivingLine.Copy(dstStock, this);
			Id = 0;
			SrcStock = srcStock;
			SrcStock.Reserve(Quantity);
			DstStock = dstStock;
			DstStock.Reserve(DstStock.Quantity);
		}

		public virtual uint Id { get; set; }

		public virtual DateTime? Exp { get; set; }

		public virtual string Period { get; set; }

		public virtual decimal? SupplierSumWithoutNds => SupplierCostWithoutNds * Quantity;

		public virtual decimal? RetailSum => RetailCost * Quantity;

		public virtual decimal Quantity { get; set; }

		public virtual Stock SrcStock { get; set; }
		public virtual Stock DstStock { get; set; }

		public virtual void UpdateQuantity(decimal quantity)
		{
			SrcStock.Release(Quantity);
			SrcStock.Reserve(quantity);
			DstStock.Release(Quantity);
			DstStock.Reserve(quantity);
			Quantity = quantity;
		}
	}
}