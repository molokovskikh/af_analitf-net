using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class UnpackingDoc : BaseNotify
	{
		private DocStatus _status;

		public UnpackingDoc()
		{
			Lines = new List<UnpackingLine>();
		}

		public UnpackingDoc(Address address)
			: this()
		{
			Date = DateTime.Now;
			Address = address;
			UpdateStat();
		}

		public virtual uint Id { get; set; }
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

		public virtual void DeleteLine(UnpackingLine line)
		{
			line.SrcStock.Release(line.SrcQuantity);
			line.DstStock.ReservedQuantity -= line.Quantity;
			Lines.Remove(line);
		}
	}
}
