using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models
{

	public class DeletedOrder : BaseNotify, IOrder, IFormattable
	{
		private string personalComment;

		public DeletedOrder()
		{}

		public DeletedOrder(Order order)
		{
			DeletedOn = DateTime.Now;
			Address = order.Address;
			Price = order.Price;
			PriceDate = order.Price is NHibernate.Proxy.INHibernateProxy ? order.CreatedOn : Price.PriceDate;
			CreatedOn = order.CreatedOn;
			LinesCount = order.LinesCount;
			Sum = order.Sum;
			Comment = order.Comment;
			PersonalComment = order.PersonalComment;
			ServerId = order.ServerId;

			Lines = order.Lines
				.Select(l => new DeletedOrderLine(this, l))
				.ToList();
		}

		public virtual uint Id { get; set; }

		public virtual uint DisplayId => (uint)ServerId;

		public virtual Price Price { get; set; }

		public virtual Address Address { get; set; }

		public virtual string Comment { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual DateTime DeletedOn { get; set; }

		public virtual DateTime PriceDate { get; set; }

		public virtual int LinesCount { get; set; }

		public virtual decimal Sum { get; set; }

		public virtual ulong ServerId { get; set; }

		public virtual string PersonalComment { get; set; }

		public virtual IList<DeletedOrderLine> Lines { get; set; }

		public virtual string PriceName => SafePrice?.Name;

		public virtual string AddressName
		{
			get
			{
				if (IsAddressExists())
					return Address.Name;
				return "";
			}
		}

		public virtual Address SafeAddress
		{
			get
			{
				if (IsAddressExists())
					return Address;
				return null;
			}
		}

		public virtual Price SafePrice
		{
			get
			{
				if (IsPriceExists())
					return Price;
				return new Price {
					Id = Price.Id
				};
			}
		}

		IEnumerable<IOrderLine> IOrder.Lines => Lines;

		public virtual string PriceLabel => $"{PriceName} от {PriceDate}";

		public virtual bool IsPriceExists()
		{
			return NHHelper.IsExists(() => String.IsNullOrEmpty(Price?.Name));
		}

		public virtual bool IsAddressExists()
		{
			return NHHelper.IsExists(() => String.IsNullOrEmpty(Address?.Name));
		}

		public virtual string ToString(string format, IFormatProvider formatProvider)
		{
			switch (format) {
				case "full":
					return $"{DisplayId} дата удаления: {DeletedOn} прайс-лист: {SafePrice?.Name} позиций: {LinesCount}";
				default:
					return base.ToString();
			}
		}
	}
}