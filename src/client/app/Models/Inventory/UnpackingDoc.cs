using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
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
			Lines = new List<UnpackingLine>();
		}

		public UnpackingDoc(Address address, User user)
			: this()
		{
			Date = DateTime.Now;
			Address = address;
			_numberprefix = user.Id.ToString() + "-";
			_new = true;
			UpdateStat();
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
		{ get { return string.Empty; } }

		public virtual uint? ServerId { get; set; }
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

		public virtual IList<UnpackingLine> Lines { get; set; }

		public virtual void Post()
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
			Timestamp = DateTime.Now;
			foreach (var line in Lines){
				line.SrcStock.ReservedQuantity -= line.SrcQuantity;
				line.DstStock.SupplyQuantity = line.DstStock.Quantity;
				line.DstStock.Timestamp = DateTime.Now;
				line.DstStock.ReservedQuantity -= line.DstStock.Quantity;
			}
		}
		public virtual void PostStockActions()
		{
			foreach (var line in Lines)
			{
				line.SrcStockAction = new StockAction(ActionType.UnpackingDoc, ActionTypeChange.Minus, line.SrcStock, this, line.SrcQuantity);
				line.DstStockAction = new StockAction(ActionType.Stock, ActionTypeChange.Plus, line.DstStock, this, line.DstStock.Quantity);
			}

		}

		//public virtual void UnPost()
		//{
		//	CloseDate = null;
		//	Status = DocStatus.NotPosted;
		//	foreach (var line in Lines)
		//	{
		//		line.SrcStock.ReservedQuantity += line.SrcQuantity;
		//		line.DstStock.CancelIncoming(line.Quantity);
		//	}
		//}

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

		public virtual void DeleteLine(UnpackingLine line)
		{
			line.SrcStock.Release(line.SrcQuantity);
			line.DstStock.ReservedQuantity -= line.Quantity;
			Lines.Remove(line);
		}
	}
}
