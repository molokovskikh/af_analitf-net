using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReturnToSupplier : BaseStatelessObject, IDataErrorInfo2
	{

//Причина возврата - выбор из списка, обязат.
//Партия № - выбор из списка, который зависит от выбора поставщика, не обязат.
//Накладная № - проставляется при выборе партии, не обязат.


		public ReturnToSupplier()
		{
			Lines = new List<ReturnToSupplierLine>();
		}

		public ReturnToSupplier(Address address)
			: this()
		{
			Department = address;
			NumDoc = "1???";
			DateDoc = DateTime.Now;
			Status = Status.Open;
			UpdateStat();
		}

		public override uint Id { get; set; }
		public virtual string NumDoc { get; set; }
		public virtual DateTime DateDoc { get; set; }
		public virtual DateTime? DateClosing { get; set; }
		public virtual Status Status { get; set; }
		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		public virtual Address Department { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual string SupplierName => Supplier.FullName;
		public virtual string DepartmentName => Department.Name;

		public virtual decimal RetailSum { get; set; }
		public virtual decimal SupplierSumWithoutNds { get; set; }
		public virtual decimal SupplierSum { get; set; }
		public virtual int PosCount { get; set; }

		public virtual IList<ReturnToSupplierLine> Lines { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				switch (columnName)
				{
					case "NumDoc":
						if (String.IsNullOrEmpty(NumDoc))
							return "Не установлен номер документа.";
						break;
					//case "DateDoc":
					//	if (String.IsNullOrEmpty(DateDoc))
					//		return "Не установлена дата.";
					//	break;
					case "Department":
						if (Department == null)
							return "Не установлен отдел.";
						break;
					case "Supplier":
						if (Supplier == null)
							return "Не установлен поставщик.";
						break;
					default:
						return "";
				}
				return "";
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { "NumDoc", "DateDoc", "Department", "Supplier" };

		public virtual void UpdateStat()
		{
			RetailSum = Lines.Sum(x => x.RetailSum);
			SupplierSumWithoutNds = Lines.Sum(x => x.SupplierSumWithoutNds);
			SupplierSum = Lines.Sum(x => x.SupplierSum);
			PosCount = Lines.Count();
		}
	}
}
