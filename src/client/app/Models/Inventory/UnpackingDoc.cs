﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class UnpackingDoc : BaseNotify, IStockDocument
	{
		private DocStatus _status;
		private bool _new;
		private uint _id;
		private string _numberprefix;
		private string _numberdoc;

		public UnpackingDoc()
		{
			Lines = new List<UnpackingDocLine>();
		}

		public UnpackingDoc(Address address, string numberprefix)
			: this()
		{
			Date = DateTime.Now;
			Address = address;
			UpdateStat();
			_numberprefix = numberprefix;
			_new = true;
		}

		public virtual uint Id
		{
			get { return _id; }
			set
			{
				_id = value;
				if (_new)
					NumberDoc = _numberprefix + Id.ToString("d8");
			}
		}
		public virtual string DisplayName { get { return "Распаковка"; } }
		public virtual string NumberDoc
		{
			get { return !String.IsNullOrEmpty(_numberdoc) ? _numberdoc : Id.ToString("d8"); }
			set { _numberdoc = value; }
		}
		public virtual string FromIn
		{ get { return string.Empty; } }
		public virtual string OutTo
		{ get { return "Покупатель"; } }
		public virtual DateTime Timestamp { get; set; }
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

		public virtual DateTime? CloseDate { get; set; }
		public virtual decimal? SrcRetailSum { get; set; }
		public virtual decimal? RetailSum { get; set; }
		public virtual string Comment { get; set; }
		public virtual decimal? Delta => RetailSum - SrcRetailSum;
		public virtual int LinesCount { get; set; }

		[Ignore]
		[Style(Description = "Непроведенный документ")]
		public virtual bool IsNotPosted => Status == DocStatus.NotPosted;

		public virtual IList<UnpackingDocLine> Lines { get; set; }

		public virtual void Post()
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
			foreach (var line in Lines){
				line.SrcStock.ReservedQuantity -= line.SrcQuantity;
				line.DstStock.Incoming(line.Quantity);
			}
		}

		public virtual void UnPost()
		{
			CloseDate = null;
			Status = DocStatus.NotPosted;
			foreach (var line in Lines)
			{
				line.SrcStock.ReservedQuantity += line.SrcQuantity;
				line.DstStock.CancelIncoming(line.Quantity);
			}
		}

		public virtual void BeforeDelete()
		{
			// с поставки наружу
			foreach (var line in Lines) {
				line.SrcStock.Release(line.SrcQuantity);
				line.DstStock.ReservedQuantity -= line.Quantity;
			}
		}

		public virtual void UpdateStat()
		{
			LinesCount = Lines.Count;
			RetailSum = Lines.Sum(x => x.RetailSum);
			SrcRetailSum = Lines.Sum(x => x.SrcRetailSum);
		}

		public virtual void DeleteLine(UnpackingDocLine line)
		{
			line.SrcStock.Release(line.SrcQuantity);
			line.DstStock.ReservedQuantity -= line.Quantity;
			Lines.Remove(line);
		}
	}

	public class UnpackingDocLine : BaseStock
	{
		public UnpackingDocLine()
		{
		}

		public UnpackingDocLine(Stock srcStock, int multiplicity)
		{
			// распаковывается одна упаковка
			var quantity = 1m;
			Stock.Copy(srcStock, this);
			var dstStock = srcStock.Copy();
			dstStock.Quantity = 0;
			dstStock.Unpacked = true;
			Quantity = dstStock.ReservedQuantity = dstStock.Multiplicity = multiplicity;
			DstStock = dstStock;

			Id = 0;
			SrcQuantity = quantity;
			SrcRetailCost = srcStock.RetailCost;
			srcStock.Reserve(quantity);
			SrcStock = srcStock;

			if (srcStock.RetailCost.HasValue)
				RetailCost = dstStock.RetailCost = getPriceForUnit(srcStock.RetailCost.Value, multiplicity);

			if (srcStock.SupplierCost.HasValue)
				dstStock.SupplierCost = getPriceForUnit(srcStock.SupplierCost.Value, multiplicity);
		}

		public virtual uint Id { get; set; }

		public virtual decimal Quantity { get; set; }

		public override decimal? RetailCost { get; set; }

		public virtual decimal? RetailSum => RetailCost * Quantity;

		public virtual decimal SrcQuantity { get; set; }

		public virtual decimal? SrcRetailCost { get; set; }

		public virtual decimal? SrcRetailSum => SrcRetailCost * SrcQuantity;

		public virtual decimal? Delta => RetailSum - SrcRetailSum;

		// признак движения распакованного товара
		public virtual bool Moved => (DstStock.Quantity + DstStock.ReservedQuantity) != DstStock.Multiplicity;

		public virtual Stock SrcStock { get; set; }
		public virtual Stock DstStock { get; set; }

		private decimal getPriceForUnit(decimal price, int multiplicity)
		{
			return Math.Floor(price * 100 / multiplicity) / 100;
		}
	}
}
