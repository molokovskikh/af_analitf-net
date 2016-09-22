using System;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using Common.Tools;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class ReturnToSupplierLine : BaseNotify, IDataErrorInfo2
	{
		public virtual uint Id { get; set; }

		public virtual uint? ProductId { get; set; }

		public virtual string Product { get; set; }

		public virtual uint? ProducerId { get; set; }

		public virtual string Producer { get; set; }

		public virtual uint ReturnToSupplierId { get; set; }

		public virtual decimal Quantity { get; set; }

		public virtual decimal? SupplierCostWithoutNds { get; set; }

		public virtual decimal SupplierSumWithoutNds => Quantity * SupplierCostWithoutNds.GetValueOrDefault();

		public virtual decimal? SupplierCost { get; set; }

		public virtual decimal SupplierSum => Quantity * SupplierCost.GetValueOrDefault();

		public virtual decimal? RetailCost { get; set; }

		public virtual decimal RetailSum => Quantity * RetailCost.GetValueOrDefault();

		public virtual Stock Stock { get; set; }

		public ReturnToSupplierLine()
		{
			
		}

		public ReturnToSupplierLine(uint returnToSupplierId)
		{
			ReturnToSupplierId = returnToSupplierId;
		}


		public virtual string this[string columnName]
		{
			get
			{
				switch (columnName)
				{
					case "Quantity":
						if (Quantity <= 0)
							return "Не установлено количество";
						if (Stock != null && Quantity > Stock.Quantity)
							return "Количество не может быть больше количества товарного остатка";
						break;
					case "Stock":
						if (Stock == null)
							return "Не установлен товарный остаток";
						break;
					default:
						return "";
				}
				return "";
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { "Quantity", "Stock" };

	}
}
