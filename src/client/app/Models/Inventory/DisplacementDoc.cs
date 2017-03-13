using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using Common.Tools;
using AnalitF.Net.Client.Config.NHibernate;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum DisplacementDocStatus
	{
		[Description("Резерв")]
		NotPosted,
		[Description("Передано")]
		Posted,
		[Description("Получено")]
		End
	}

	public class DisplacementDoc : BaseStatelessObject, IDataErrorInfo2, IStockDocument
	{
		private bool _new;
		private uint _id;
		private string _numberprefix;

		public DisplacementDoc()
		{
			DisplayName = "Накладная перемещения";
			Lines = new List<DisplacementLine>();
		}

		public DisplacementDoc(Address address, string numberprefix)
			: this()
		{
			DisplayName = "Накладная перемещения";
			_numberprefix = numberprefix;
			_new = true;
			Address = address;
			Date = DateTime.Now;
			Status = DisplacementDocStatus.NotPosted;
			UpdateStat();
		}

		private DisplacementDocStatus _status;

		public override uint Id
		{
			get { return _id; }
			set
			{
				_id = value;
				if (_new)
					Number = _numberprefix + Id.ToString("d8");
			}
		}
		public virtual string DisplayName { get; set; }
		public virtual string Number { get; set; }
		public virtual string FromIn
		{
			get
			{
				return string.Empty;
			}
		}
		public virtual string OutTo
		{ get { return DstAddressName; } }

		public virtual DateTime Timestamp { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual DateTime? CloseDate { get; set; }
		public virtual DisplacementDocStatus Status
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
		public virtual bool IsNotPosted => Status == DisplacementDocStatus.NotPosted;

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual Address Address { get; set; }
		public virtual string AddressName => Address?.Name;

		public virtual Address DstAddress { get; set; }
		public virtual string DstAddressName => DstAddress?.Name;

		public virtual decimal SupplierSum { get; set; }
		public virtual int PosCount { get; set; }

		public virtual IList<DisplacementLine> Lines { get; set; }

		public virtual string Comment { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Address) && Address == null)
					return "Поле 'Отправитель' должно быть заполнено";
				if (columnName == nameof(DstAddress) && DstAddress == null)
					return "Поле 'Получатель' должно быть заполнено";
				if (DstAddress?.Id == Address?.Id)
				{
					return "'Отправитель' и 'Получатель' не могут совпадать";
				}
				return null;
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { nameof(Address), nameof(DstAddress) };

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DisplacementDocStatus.Posted;
			foreach (var line in Lines) {
				session.Save(line.SrcStock.DisplacementTo(this, line.Quantity));
				session.Save(line.DstStock.DisplacementFrom(this, line.Quantity));
			}
		}

		public virtual void UnPost(ISession session)
		{
			CloseDate = null;
			Status = DisplacementDocStatus.NotPosted;
			foreach (var line in Lines) {
				session.Save(line.SrcStock.CancelDisplacementTo(this, line.Quantity));
				session.Save(line.DstStock.CancelDisplacementFrom(this, line.Quantity));
			}
		}

		public virtual void End(ISession session)
		{
			Status = DisplacementDocStatus.End;
			foreach (var line in Lines)
				line.DstStock.Incoming(line.Quantity);
		}

		// для теста
		public virtual void ReEnd(ISession session)
		{
			Status = DisplacementDocStatus.Posted;
			foreach (var line in Lines)
				line.DstStock.CancelIncoming(line.Quantity);
		}

		public virtual void BeforeDelete()
		{
			foreach (var line in Lines)
				line.SrcStock.Release(line.Quantity);
		}

		public virtual void UpdateStat()
		{
			SupplierSum = Lines.Sum(x => x.SupplierSum);
			PosCount = Lines.Count();
		}
	}
}
