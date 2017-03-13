using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class WriteoffDoc : BaseNotify, IDataErrorInfo2, IStockDocument
	{
		private DocStatus _status;
		private string _number { get; set; }
		private string _numberprefix { get; set; }

		public WriteoffDoc()
		{
			DisplayName = "��������";
			Lines = new List<WriteoffLine>();
		}

		public WriteoffDoc(Address address, string numberprefix)
			: this()
		{
			
			Date = DateTime.Now;
			Address = address;
			_numberprefix = numberprefix;
			DisplayName = "��������";
		}

		public virtual uint Id { get; set; }
		public virtual string DisplayName { get; set; }
		public virtual string Number
		{
			get
			{
				return _number;
			}
			set { _number = _numberprefix + Id.ToString("d8"); }
		}
		public virtual string FromIn
		{ get { return string.Empty; } }
		public virtual string OutTo
		{ get { return string.Empty; } }
		public virtual DateTime Timestamp { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual Address Address { get; set; }
		public virtual string AddressName => Address?.Name;
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
		[Style(Description = "������������� ��������")]
		public virtual bool IsNotPosted => Status == DocStatus.NotPosted;

		public virtual IList<WriteoffLine> Lines { get; set; }

		public virtual string Comment { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				if ((columnName == nameof(Reason)) && (Reason == null))
					return "���� '�������' ������ ���� ���������";
				if ((columnName == nameof(Address)) && (Address == null))
					return "���� '�����' ������ ���� ���������";
				return null;
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] {nameof(Address), nameof(Reason) };

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
			foreach (var line in Lines)
				session.Save(line.Stock.ApplyReserved(this, line.Quantity));
		}

		public virtual void UpdateStat()
		{
			LinesCount = Lines.Count;
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplySumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplySum = Lines.Sum(x => x.Quantity*x.SupplierCost);
		}
	}
}