﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NHibernate;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum DocStatus
	{
		[Description("Не проведен")] NotPosted,
		[Description("Проведен")] Posted
	}

	public class InventoryDoc : BaseNotify, IEditableObject, IDataErrorInfo2, IStockDocument
	{
		private DocStatus _status;
		private bool _new;
		private uint _id;
		private string _numberprefix;
		private string _numberdoc;

		public InventoryDoc()
		{
			Lines = new List<InventoryLine>();
		}

		public InventoryDoc(Address address, User user)
			: this()
		{
			Date = DateTime.Now;
			_numberprefix = user.Id.ToString() + "-";
			Address = address;
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
		public virtual string DisplayName { get { return "Инвентаризация"; } }
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

		public virtual DateTime? CloseDate { get; set; }
		public virtual decimal? SupplySumWithoutNds { get; set; }
		public virtual decimal? SupplySum { get; set; }
		public virtual decimal? RetailSum { get; set; }
		public virtual decimal LinesCount { get; set; }
		public virtual string Comment { get; set; }

		public virtual IList<InventoryLine> Lines { get; set; }

		public virtual string[] FieldsForValidate => new[] { nameof(Address) };

		public virtual string Error { get; }

		public virtual string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Address) && Address == null)
				{
					return "Поле 'Адрес' должно быть заполнено";
				}
				return null;
			}
		}

		public virtual void Post()
		{
			CloseDate = SystemTime.Now();
			Timestamp = SystemTime.Now();
			Status = DocStatus.Posted;
			// с поставки на склад
			foreach (var line in Lines) {
				line.Stock.Incoming(line.Quantity);
				// если сток создан вместе со строкой - меняется статус, иначе - остаётся какой был
				if (line.StockIsNew)
				{
					line.Stock.SupplyQuantity = line.Stock.Quantity;
					line.Stock.Status = StockStatus.Available;
					line.Stock.Timestamp = DateTime.Now;
				}
			}
		}

		public virtual void UnPost()
		{
			CloseDate = null;
			Status = DocStatus.NotPosted;
			// со склада в поставку
			foreach (var line in Lines) {
				if (line.StockIsNew)
					line.Stock.Status = StockStatus.InTransit;
				line.Stock.CancelIncoming(line.Quantity);
			}
		}

		public virtual void BeforeDelete(ISession session)
		{
			// с поставки наружу
			foreach (var line in Lines)
				session.Save(line.Stock.CancelInventoryDoc(this, line.Quantity));
		}

		public virtual void UpdateStat()
		{
			LinesCount = Lines.Count;
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplySumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplySum = Lines.Sum(x => x.Quantity*x.SupplierCost);
		}

		public virtual void BeginEdit()
		{
		}

		public virtual void EndEdit()
		{
		}

		public virtual void CancelEdit()
		{
		}
	}
}
