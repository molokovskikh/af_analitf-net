using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalitF.Net.Client.Models
{
	public class SentOrder
	{
		public SentOrder()
		{}

		public SentOrder(Order order)
		{
			SentOn = DateTime.Now;

			Address = order.Address;
			Price = order.Price;
			CreatedOn = order.CreatedOn;
			LinesCount = order.LinesCount;
			Sum = order.Sum;
			Comment = order.Comment;
			PersonalComment = order.PersonalComment;

			Lines = order.Lines
				.Select(l => new SentOrderLine(this, l))
				.ToList();
		}

		public virtual uint Id { get; set; }

		public virtual Address Address { get; set; }

		public virtual Price Price { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual DateTime SentOn { get; set; }

		public virtual int LinesCount { get; set; }

		public virtual decimal Sum { get; set; }

		public virtual string Comment { get; set; }

		public virtual string PersonalComment { get; set; }

		public virtual IList<SentOrderLine> Lines { get; set; }
	}
}