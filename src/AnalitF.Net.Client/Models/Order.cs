using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AnalitF.Net.Client.Models
{
	public class Order : INotifyPropertyChanged
	{
		private decimal sum;

		public Order()
		{
			Lines = new List<OrderLine>();
		}

		public Order(Price price, Address address)
			: this()
		{
			Address = address;
			Price = price;
			CreatedOn = DateTime.Now;
		}

		public virtual uint Id { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual Address Address { get; set; }

		public virtual Price Price { get; set; }

		public virtual int LinesCount { get; set; }

		public virtual decimal Sum
		{
			get { return sum; }
			set
			{
				sum = value;
				OnPropertyChanged("Sum");
			}
		}

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
			Lines.Remove(line);
			Sum = Lines.Sum(l => l.Sum);
			LinesCount = Lines.Count;
		}

		public virtual void AddLine(OrderLine line)
		{
			Lines.Add(line);
			Sum = Lines.Sum(l => l.Sum);
			LinesCount = Lines.Count;
		}

		public virtual void AddLine(Offer offer, uint count)
		{
			AddLine(new OrderLine(this, offer, count));
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}