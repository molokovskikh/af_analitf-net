using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;

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

		uint Count { get; }

		decimal Sum { get; }

		uint? RequestRatio { get; }

		decimal? RegistryCost { get; }

		decimal? MaxProducerCost { get; }

		decimal? ProducerCost { get; }

		decimal? SupplierMarkup { get; }

		uint? NDS { get; }

		string Quantity { get; }

		string Comment { get; set; }
	}

	public interface IOrder
	{
		uint Id { get; }

		DateTime CreatedOn { get; }

		Price Price { get; }

		Address Address { get; }

		decimal Sum { get; }

		string Comment { get; set; }

		string PersonalComment { get; set; }

		string PriceLabel { get; }

		IEnumerable<IOrderLine> Lines { get; }
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
				OnPropertyChanged("PersonalComment");
			}
		}

		public virtual IList<SentOrderLine> Lines { get; set; }

		IEnumerable<IOrderLine> IOrder.Lines
		{
			get { return Lines; }
		}

		public virtual string PriceLabel
		{
			get
			{
				return String.Format("{0} от {1}", Price, PriceDate);
			}
		}
	}
}