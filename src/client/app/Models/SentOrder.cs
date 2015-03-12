using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;

namespace AnalitF.Net.Client.Models
{
	public interface IOrderLine
	{
		uint ProductSynonymId { get; }

		string ProductSynonym { get; }

		uint? ProducerSynonymId { get; }

		string ProducerSynonym { get; }

		string Producer { get; }

		decimal? MinOrderSum { get; }

		uint? MinOrderCount { get; }

		uint ProductId { get; }

		uint? ProducerId { get; }

		string Code { get; }

		string Period { get; }

		decimal Cost { get; }

		decimal ResultCost { get; }

		uint Count { get; }

		uint? RequestRatio { get; }

		decimal? RegistryCost { get; }

		decimal? MaxProducerCost { get; }

		decimal? ProducerCost { get; }

		decimal? SupplierMarkup { get; }

		uint? NDS { get; }

		string Quantity { get; }

		string Comment { get; set; }

		decimal MixedCost { get; }

		decimal Sum { get; }

		decimal MixedSum { get; }

		bool Junk { get; }

		void Configure(User user);
	}

	public interface IOrder
	{
		uint Id { get; }

		uint DisplayId { get; }

		DateTime CreatedOn { get; }

		Price Price { get; }

		Address Address { get; }

		decimal Sum { get; }

		string Comment { get; set; }

		string PersonalComment { get; set; }

		string PriceLabel { get; }

		IEnumerable<IOrderLine> Lines { get; }

		string PriceName { get; }

		string AddressName { get; }

		Price SafePrice { get; }

		bool IsPriceExists();

		bool IsAddressExists();
	}

	public class SentOrder : BaseNotify, IOrder
	{
		private string personalComment;

		public SentOrder()
		{}

		public SentOrder(Order order)
		{
			SentOn = DateTime.Now;
			Address = order.Address;
			Price = order.Price;
			PriceDate = Price.PriceDate;
			CreatedOn = order.CreatedOn;
			LinesCount = order.LinesCount;
			Sum = order.Sum;
			Comment = order.Comment;
			PersonalComment = order.PersonalComment;
			ServerId = order.ServerId;

			Lines = order.Lines
				.Select(l => new SentOrderLine(this, l))
				.ToList();
		}

		public virtual uint Id { get; set; }

		public virtual uint DisplayId
		{
			get { return (uint)ServerId; }
		}

		public virtual Price Price { get; set; }

		public virtual Address Address { get; set; }

		public virtual string Comment { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual DateTime SentOn { get; set; }

		public virtual DateTime PriceDate { get; set; }

		public virtual int LinesCount { get; set; }

		public virtual decimal Sum { get; set; }

		public virtual ulong ServerId { get; set; }

		public virtual string PersonalComment
		{
			get { return personalComment; }
			set
			{
				personalComment = value;
				OnPropertyChanged();
			}
		}

		public virtual IList<SentOrderLine> Lines { get; set; }

		public virtual string PriceName
		{
			get { return SafePrice != null ? SafePrice.Name : ""; }
		}

		public virtual string AddressName
		{
			get
			{
				if (IsAddressExists())
					return Address.Name;
				return "";
			}
		}

		public virtual Price SafePrice
		{
			get
			{
				if (Price == null)
					return null;
				if (IsPriceExists())
					return Price;
				return new Price {
					Id = Price.Id
				};
			}
		}

		IEnumerable<IOrderLine> IOrder.Lines
		{
			get { return Lines; }
		}

		public virtual string PriceLabel
		{
			get
			{
				return String.Format("{0} от {1}", PriceName, PriceDate);
			}
		}

		public virtual bool IsPriceExists()
		{
			bool priceNotFound;
			try {
				priceNotFound = Price == null || Price.Name == "";
			}
			catch (ObjectNotFoundException) {
				priceNotFound = true;
			}
			return !priceNotFound;
		}

		public virtual bool IsAddressExists()
		{
			bool addressNotFound;
			try {
				addressNotFound = Address == null || Address.Name == "";
			}
			catch (ObjectNotFoundException) {
				addressNotFound = true;
			}
			return !addressNotFound;
		}

		public override string ToString()
		{
			return String.Format("Заявка поставщика {0} на сумму {1}", Price, Sum);
		}
	}
}