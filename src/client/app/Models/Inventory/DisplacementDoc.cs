﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum DisplacementDocStatus
	{
		[Description("Резерв")]
		Opened,
		[Description("Передано")]
		Closed,
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
			Status = DisplacementDocStatus.Opened;
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

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual Address Address { get; set; }
		public virtual string AddressName => Address.Name;

		public virtual Address Recipient { get; set; }
		public virtual string RecipientName => Recipient.Name;

		public virtual decimal SupplierSum { get; set; }
		public virtual int PosCount { get; set; }

		public virtual IList<DisplacementLine> Lines { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Address) && Address == null)
				{
					return "Поле 'Отправитель' должно быть заполнено";
				}
				if (columnName == nameof(Recipient) && Recipient == null)
				{
					return "Поле 'Получатель' должно быть заполнено";
				}
				return null;
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { nameof(Address), nameof(Recipient) };

		public virtual void Close(ISession session)
		{
			CloseDate = DateTime.Now;
			Status = DisplacementDocStatus.Closed;
			foreach (var line in Lines)
				session.Save(line.Stock.Displacement(line.Quantity));
		}

		public virtual void ReOpen(ISession session)
		{
			CloseDate = null;
			Status = DisplacementDocStatus.Opened;
			foreach (var line in Lines)
				session.Save(line.Stock.CancelDisplacement(line.Quantity));
		}

		public virtual void End(ISession session)
		{
			CloseDate = null;
			Status = DisplacementDocStatus.End;
			foreach (var line in Lines)
				session.Save(line.Stock.EndDisplacement(line.Quantity));
		}

		public virtual void BeforeDelete()
		{
			foreach (var line in Lines)
				line.Stock.Release(line.Quantity);
		}

		public virtual void UpdateStat()
		{
			SupplierSum = Lines.Sum(x => x.SupplierSum);
			PosCount = Lines.Count();
		}
	}
}
