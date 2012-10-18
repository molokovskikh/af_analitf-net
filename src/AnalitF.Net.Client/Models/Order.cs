using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalitF.Net.Client.Models
{
	public class Order
	{
		public Order()
		{
			Lines = new List<OrderLine>();
		}

		public Order(Price price)
			: this()
		{
			Price = price;
			CreatedOn = DateTime.Now;
		}

		public virtual uint Id { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual Price Price { get; set; }

		public virtual int LinesCount { get; set; }

		public virtual decimal Sum { get; set; }

		public virtual decimal MonthlyOrderSum { get; set; }

		public virtual decimal WeeklyOrderSum { get; set; }

		public virtual bool Send { get; set; }

		public virtual bool Frozen { get; set; }

		public virtual string Comment { get; set; }

		public virtual string PersonalComment { get; set; }

		public virtual IList<OrderLine> Lines { get; set; }

		public virtual bool Valid
		{
			get { return true; }
		}

		public virtual bool IsEmpty
		{
			get { return Lines.Count == 0; }
		}

		public virtual void RemoveLine(OrderLine line)
		{
			Sum = Lines.Sum(l => l.Sum);
			LinesCount = Lines.Count;
			Lines.Remove(line);
		}

		public virtual void AddLine(OrderLine line)
		{
			Sum = Lines.Sum(l => l.Sum);
			LinesCount = Lines.Count;
			Lines.Add(line);
		}
	}
}