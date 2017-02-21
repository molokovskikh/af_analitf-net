using System;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
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

		public virtual uint? ServerDocId { get; set; }

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