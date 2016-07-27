using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum CheckType
	{
		[Description("Продажа покупателю")] SaleBuyer,
		[Description("Возврат по чеку")] CheckReturn,
	}
	public enum PaymentType
	{
		[Description("Наличный рубль")] Cash,
	}
	public enum Status
	{
		[Description("Закрыт")] Closed,
		[Description("Открыт")] Open,
	}
	public enum SaleType
	{
		[Description("Полная стоимость")] FullCost,
	}
	public class Check
	{
		public Check()
		{
			Lines = new List<CheckLine>();
		}

		public Check(uint number)
		{
			Lines = new List<CheckLine>();
			Number = number;
		}

		public virtual uint Id { get; set; }
		public virtual CheckType CheckType { get; set; }
		public virtual uint Number { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual DateTime ChangeOpening { get; set; }
		public virtual Status Status { get; set; }

		public virtual string Clerk { get; set; } //Пока пусть будет строка
		public virtual Address Department { get; set; }

		public virtual uint KKM { get; set; }
		public virtual PaymentType PaymentType { get; set; }
		public virtual SaleType SaleType { get; set; }
		public virtual uint Discont { get; set; }
		public virtual uint ChangeId { get; set; }
		public virtual uint ChangeNumber { get; set; }
		public virtual bool Cancelled { get; set; }
		public virtual decimal Sum { get; set; }
		public virtual decimal RetailSum { get; set; }
		public virtual decimal DiscontSum  { get; set; }

		//Эти поля были пустыми
		public virtual string SaleCheck { get; set; }
		public virtual string DiscountCard { get; set; }
		public virtual string Recipe { get; set; }
		public virtual string Agent { get; set; }

		public virtual IList<CheckLine> Lines { get; set; }

		public virtual Stock[] ToStocks()
		{
			return Lines.Select(x => new Stock {
				ProductId = x.ProductId,
				ProducerId = x.ProducerId,
				Count = x.Quantity,
				Cost = x.Cost,
				RetailCost = x.RetailCost,
			}).ToArray();
		}
	}
}
