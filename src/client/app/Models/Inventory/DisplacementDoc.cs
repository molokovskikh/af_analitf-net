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

	public class DisplacementDoc : BaseStatelessObject, IDataErrorInfo2
	{
		public DisplacementDoc()
		{
			Lines = new List<DisplacementLine>();
		}

		public DisplacementDoc(Address address)
			: this()
		{
			Address = address;
			Date = DateTime.Now;
			Status = DisplacementDocStatus.NotPosted;
			UpdateStat();
		}

		private DisplacementDocStatus _status;

		public override uint Id { get; set; }
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
		public virtual string AddressName => Address.Name;

		public virtual Address DstAddress { get; set; }
		public virtual string DstAddressName => DstAddress.Name;

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
				if (columnName == nameof(Lines) && !Lines.Any())
					return "Документ не может быть пустым";
				return null;
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { nameof(Address), nameof(DstAddress), nameof(Lines) };

		public virtual void Post(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DisplacementDocStatus.Posted;
			foreach (var line in Lines) {
				session.Save(line.SrcStock.DisplacementTo(line.Quantity));
				session.Save(line.DstStock.DisplacementFrom(line.Quantity));
			}
		}

		public virtual void UnPost(ISession session)
		{
			CloseDate = null;
			Status = DisplacementDocStatus.NotPosted;
			foreach (var line in Lines) {
				session.Save(line.SrcStock.CancelDisplacementTo(line.Quantity));
				session.Save(line.DstStock.CancelDisplacementFrom(line.Quantity));
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
