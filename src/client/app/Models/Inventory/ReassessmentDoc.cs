using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReassessmentDoc : BaseNotify, IDataErrorInfo2, IStockDocument
	{
		private DocStatus _status;
		private bool _new;
		private uint _id;
		private string _numberprefix;
		private string _numberdoc;

		public ReassessmentDoc()
		{
			Lines = new List<ReassessmentLine>();
		}

		public ReassessmentDoc(Address address, User user)
			: this()
		{
			Date = DateTime.Now;
			Address = address;
			_numberprefix = user.Id.ToString() + "-";
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
		public virtual string DisplayName { get { return "Переоценка"; } }
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
		public virtual string AddressName => Address?.Name;

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

		public virtual string Comment { get; set; }

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
			Timestamp = DateTime.Now;
			foreach (var line in Lines) {
				session.Save(line.SrcStock.ApplyReserved(this, line.Quantity));

				line.DstStock.Timestamp = DateTime.Now;
				line.DstStock.Quantity += line.Quantity;
				session.Save(line.DstStock);
				session.Save(new StockAction(ActionType.Stock, ActionTypeChange.Plus, line.DstStock, this, line.DstStock.Quantity));
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
}