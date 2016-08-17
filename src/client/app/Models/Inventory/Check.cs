using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Helpers;

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
	public class Check:BaseNotify
	{
		public Check()
		{
			Lines = new List<CheckLine>();
		}

		public virtual uint Id { get; set; }
		public virtual CheckType CheckType { get; set; }
		public virtual uint Number { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual DateTime ChangeOpening { get; set; }
		public virtual Status Status { get; set; }

		public virtual string Clerk { get; set; } //Пока пусть будет строка
		public virtual Address Department { get; set; }

		public virtual string KKM { get; set; }
		public virtual PaymentType PaymentType { get; set; }
		public virtual SaleType SaleType { get; set; }
		public virtual uint Discont { get; set; }
		public virtual uint ChangeId { get; set; }
		public virtual uint ChangeNumber { get; set; }
		[Style(Description = "\"Аннулирован\"")]
		public virtual bool Cancelled { get; set; }
		public virtual decimal Sum => RetailSum - DiscontSum;
		private decimal retailSum;
		public virtual decimal RetailSum
		{
			get { return retailSum; }
			set
			{
				retailSum = value;
				OnPropertyChanged();
				OnPropertyChanged("IsInvalid");
				OnPropertyChanged("IsOverLimit");
			}
		}
		private decimal discontSum;
		public virtual decimal DiscontSum
		{
			get { return discontSum; }
			set
			{
				discontSum = value;
				OnPropertyChanged();
				OnPropertyChanged("IsInvalid");
				OnPropertyChanged("IsOverLimit");
			}
		}

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

		public virtual void UpdateStat()
		{
			RetailSum = Lines.Sum(l => l.RetailSum);
			DiscontSum = Lines.Sum(l => l.DiscontSum);
		}
	}
}
