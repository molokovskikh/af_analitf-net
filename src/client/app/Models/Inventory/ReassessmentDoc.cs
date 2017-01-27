using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
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
		public virtual decimal? SrcRetailSum { get; set; }
		public virtual decimal? SupplySumWithoutNds { get; set; }
		public virtual decimal? SupplySum { get; set; }
		public virtual decimal? RetailSum { get; set; }
		public virtual decimal? Delta => RetailSum - SrcRetailSum;
		public virtual int LinesCount { get; set; }

		[Ignore]
		[Style(Description = "Непроведенный документ")]
		public virtual bool IsNotPosted => Status == DocStatus.NotPosted;

		public virtual IList<ReassessmentLine> Lines { get; set; }

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
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
			SrcRetailSum = Lines.Sum(x => x.SrcStock.RetailCost * x.Quantity);
		}

		public virtual string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Address) && Address == null)
					return "Поле 'Адрес' должно быть заполнено";
				return null;
			}
		}

		public virtual string Error { get; protected set; }

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
		private decimal? _srcRetailMarkup;
		private decimal? _srcRetailCost;
		private decimal _quantity;
		private bool _selected;
		private decimal? _retailCost;
		private decimal? _retailMarkup;

		public ReassessmentLine()
		{
		}

		public ReassessmentLine(Stock srcStock, Stock dstStock)
		{
			Stock.Copy(dstStock, this);
			Id = 0;
			SrcStock = srcStock;
			SrcStock.Reserve(Quantity);
			SrcRetailCost = srcStock.RetailCost;
			SrcRetailMarkup = srcStock.RetailMarkup;

			DstStock = dstStock;
			DstStock.Reserve(DstStock.Quantity);
		}

		public virtual uint Id { get; set; }

		public virtual DateTime? Exp { get; set; }

		public virtual string Period { get; set; }

		public virtual decimal? SupplierSumWithoutNds => SupplierCostWithoutNds * Quantity;

		public override decimal? RetailCost
		{
			get { return _retailCost; }
			set
			{
				if (_retailCost == value)
					return;
				_retailCost = value;
				OnPropertyChanged(nameof(RetailSum));
				OnPropertyChanged();
			}
		}

		public override decimal? RetailMarkup
		{
			get { return _retailMarkup; }
			set
			{
				if (_retailMarkup == value)
					return;
				_retailMarkup = value;
				OnPropertyChanged(nameof(RetailSum));
				OnPropertyChanged();
			}
		}

		public virtual decimal? RetailSum => RetailCost * Quantity;

		public virtual decimal? SrcRetailMarkup
		{
			get { return _srcRetailMarkup; }
			set
			{
				if (_srcRetailMarkup == value)
					return;
				_srcRetailMarkup = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SrcRetailSum));
			}
		}

		public virtual decimal? SrcRetailCost
		{
			get { return _srcRetailCost; }
			set
			{
				if (_srcRetailCost == value)
					return;
				_srcRetailCost = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SrcRetailSum));
			}
		}

		public virtual decimal? SrcRetailSum => Quantity * SrcRetailCost;

		public virtual decimal Quantity
		{
			get { return _quantity; }
			set
			{
				if (_quantity == value)
					return;
				_quantity = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SrcRetailSum));
				OnPropertyChanged(nameof(RetailSum));
			}
		}

		public virtual Stock SrcStock { get; set; }
		public virtual Stock DstStock { get; set; }

		[Ignore]
		public virtual bool Selected
		{
			get { return _selected; }
			set
			{
				if (Selected != value) {
					_selected = value;
					OnPropertyChanged();
				}
			}
		}

		public virtual void UpdateDst(Stock stock)
		{
			DstStock.RetailCost = stock.RetailCost;
			DstStock.RetailMarkup = stock.RetailMarkup;
			RetailCost = stock.RetailCost;
			OnPropertyChanged(nameof(RegistryCost));
			RetailMarkup = stock.RetailMarkup;
			OnPropertyChanged(nameof(RetailMarkup));
			OnPropertyChanged(nameof(RetailSum));

			var quantity = stock.Quantity;
			SrcStock.Release(Quantity);
			SrcStock.Reserve(quantity);
			DstStock.Release(Quantity);
			DstStock.Reserve(quantity);
			Quantity = quantity;
		}
	}
}