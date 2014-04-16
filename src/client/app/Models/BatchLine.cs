using System;
using System.Collections.Generic;
using System.ComponentModel;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Models
{
	[Flags]
	public enum ItemToOrderStatus
	{
		Ordered = 0x01, // Позиция заказана
		NotOrdered = 0x02, // Позиция не заказана
		MinimalCost = 0x04, // Заказан по минимальной цене
		NotEnoughQuantity = 0x08, // Не заказан по причине нехватки количества
		OffersExists = 0x10, // Предложения для данной позиции имелись
	}

	public class BatchLine : BaseStatelessObject
	{
		private Lazy<Dictionary<string, string>> lazyFields;
		private OrderLine line;

		public BatchLine()
		{
			lazyFields = new Lazy<Dictionary<string, string>>(() => {
				if (String.IsNullOrEmpty(ServiceFields))
					return new Dictionary<string, string>();
				try {
					return JsonConvert.DeserializeObject<Dictionary<string, string>>(ServiceFields);
				}
				catch(Exception) {
					return new Dictionary<string, string>();
				}
			});
		}

		public BatchLine(OrderLine orderLine)
			: this()
		{
			Address = orderLine.Order.Address;
			ExportLineId = orderLine.ExportId;
			ProductId = orderLine.ProductId;
			CatalogId = orderLine.CatalogId;
			ProductSynonym = orderLine.ProductSynonym;

			ProducerId = orderLine.ProducerId;
			ProducerSynonym = orderLine.ProducerSynonym;

			Quantity = orderLine.Count;
			Status = ItemToOrderStatus.Ordered;
		}

		public BatchLine(Catalog catalog, Address address)
		{
			Address = address;
			ProductSynonym = catalog.FullName;
			ProductId = catalog.Id;
			CatalogId = catalog.Id;
		}

		public override uint Id { get; set; }

		public virtual Address Address { get; set; }

		public virtual string ProductSynonym { get; set; }
		public virtual uint? ProductId { get; set; }
		public virtual uint? CatalogId { get; set; }

		public virtual string ProducerSynonym { get; set; }
		public virtual uint? ProducerId { get; set; }
		public virtual string Producer { get; set; }
		public virtual uint Quantity { get; set; }
		public virtual string Comment { get; set;}
		public virtual ItemToOrderStatus Status { get; set; }

		[Ignore]
		public virtual OrderLine Line
		{
			get
			{
				return line;
			}
			set
			{
				if (line == value)
					return;
				if (value == null)
					line.PropertyChanged -= TrackLine;
				else
					value.PropertyChanged += TrackLine;
					
				line = value;
			}
		}

		public virtual string ServiceFields { get; set; }
		public virtual uint? ExportLineId { get; set; }

		public virtual string MixedProduct
		{
			get
			{
				return Line == null ? ProductSynonym : Line.ProductSynonym;
			}
		}

		public virtual string MixedProducer
		{
			get
			{
				return Line == null ? ProducerSynonym : Line.ProducerSynonym;
			}
		}

		public virtual string PriceName
		{
			get { return Line == null ? "" : Line.Order.Price.Name; }
		}

		public virtual uint MixedCount
		{
			get
			{
				return Line == null ? Quantity : Line.Count;
			}
		}

		public virtual string HasProducerLabel
		{
			get { return ProducerId == null ? "Нет" : "Да"; }
		}

		public virtual string HasOrderLineLabel
		{
			get { return  Line == null ? "Нет" : "Да"; }
		}

		[Style(Description = "Не заказанные")]
		public virtual bool IsNotOrdered
		{
			get { return Line == null; }
		}

		[Style(Description = "Минимальная цена")]
		public virtual bool IsMinCost
		{
			get { return Status.HasFlag(ItemToOrderStatus.MinimalCost); }
		}

		[Style("MixedProduct", Description = "Присутствует в замороженных заказах"), Ignore]
		public virtual bool ExistsInFreezed
		{
			get; set;
		}

		public virtual Dictionary<string, string> ParsedServiceFields
		{
			get
			{
				return lazyFields.Value;
			}
		}

		private void TrackLine(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Count")
				OnPropertyChanged("MixedCount");
		}

		public override string ToString()
		{
			return string.Format("Comment: {0}, ProductId: {1}", Comment, ProductId);
		}
	}
}