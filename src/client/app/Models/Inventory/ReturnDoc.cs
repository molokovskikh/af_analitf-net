﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using Common.Tools;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReturnDoc : BaseStatelessObject, IDataErrorInfo2, IStockDocument
	{
		private bool _new;
		private uint _id;
		private string _numberprefix;
		private string _numberdoc;

		public ReturnDoc()
		{
			Lines = new List<ReturnLine>();
		}

		public ReturnDoc(Address address, User user)
			: this()
		{
			Address = address;
			Date = DateTime.Now;
			Status = DocStatus.NotPosted;
			_numberprefix = user.Id.ToString() + "-";
			_new = true;
			UpdateStat();
		}

		private DocStatus _status;

		public override uint Id
		{
			get { return _id; }
			set
			{
				_id = value;
				if (_new)
					NumberDoc = _numberprefix + Id.ToString("d8");
			}
		}
		public virtual string DisplayName { get { return "Возврат поставщику"; } }
		public virtual string NumberDoc
		{
			get { return !String.IsNullOrEmpty(_numberdoc) ? _numberdoc : Id.ToString("d8"); }
			set { _numberdoc = value; }
		}
		public virtual string FromIn
		{ get { return string.Empty; } }
		public virtual string OutTo
		{ get { return SupplierName; } }


		public virtual uint? ServerId { get; set; }
		public virtual DateTime Timestamp { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual DateTime? CloseDate { get; set; }
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

		[Ignore]
		[Style(Description = "Непроведенный документ")]
		public virtual bool IsNotPosted => Status == DocStatus.NotPosted;

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual Address Address { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual string SupplierName => Supplier.FullName;
		public virtual string AddressName => Address?.Name;

		public virtual decimal RetailSum { get; set; }
		public virtual decimal SupplierSumWithoutNds { get; set; }
		public virtual decimal SupplierSum { get; set; }
		public virtual int PosCount { get; set; }

		public virtual string Comment { get; set; }

		public virtual IList<ReturnLine> Lines { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Supplier) && Supplier == null)
					return "Поле 'Поставщик' должно быть заполнено";
				if (columnName == nameof(Address) && Address == null)
					return "Поле 'Адрес' должно быть заполнено";
				return null;
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { nameof(Address), nameof(Supplier) };

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
			Timestamp = DateTime.Now;
			foreach (var line in Lines)
				session.Save(line.Stock.ReturnToSupplier(this, line.Quantity));
		}

		public virtual void UnPost(ISession session)
		{
			CloseDate = null;
			Status = DocStatus.NotPosted;
			foreach (var line in Lines)
				session.Save(line.Stock.CancelReturnToSupplier(this, line.Quantity));
		}

		public virtual void BeforeDelete()
		{
			foreach (var line in Lines)
				line.Stock.Release(line.Quantity);
		}

		public virtual void UpdateStat()
		{
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplierSumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplierSum = Lines.Sum(x => x.SupplierSum);
			PosCount = Lines.Count();
		}
	}
}
