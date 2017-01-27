using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using Common.Tools;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReturnToSupplier : BaseStatelessObject, IDataErrorInfo2
	{
		public ReturnToSupplier()
		{
			Lines = new List<ReturnToSupplierLine>();
		}

		public ReturnToSupplier(Address address)
			: this()
		{
			Address = address;
			Date = DateTime.Now;
			Status = DocStatus.NotPosted;
			UpdateStat();
		}

		private DocStatus _status;

		public override uint Id { get; set; }
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
		public virtual string AddressName => Address.Name;

		public virtual decimal RetailSum { get; set; }
		public virtual decimal SupplierSumWithoutNds { get; set; }
		public virtual decimal SupplierSum { get; set; }
		public virtual int PosCount { get; set; }

		public virtual string Comment { get; set; }

		public virtual IList<ReturnToSupplierLine> Lines { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Supplier) && Supplier == null)
				{
					return "Поле 'Поставщик' должно быть заполнено";
				}
				if (columnName == nameof(Address) && Address == null)
				{
					return "Поле 'Адрес' должно быть заполнено";
				}
				return null;
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { nameof(Address), nameof(Supplier) };

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DocStatus.Posted;
			foreach (var line in Lines)
				session.Save(line.Stock.ReturnToSupplier(line.Quantity));
		}

		public virtual void UnPost(ISession session)
		{
			CloseDate = null;
			Status = DocStatus.NotPosted;
			foreach (var line in Lines)
				session.Save(line.Stock.CancelReturnToSupplier(line.Quantity));
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
