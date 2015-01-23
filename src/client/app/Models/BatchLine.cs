using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

	public class BatchLineView : BaseNotify
	{
		public BatchLineView(BatchLine batchline, OrderLine orderline)
		{
			BatchLine = batchline;
			OrderLine = orderline;
			BatchLine.PropertyChanged += (sender, args) => {
				if (args.PropertyName == "Quantity")
					OnPropertyChanged("Count");
			};
			if (OrderLine != null) {
				OrderLine.PropertyChanged += (sender, args) => {
					if (args.PropertyName == "Count")
						OnPropertyChanged("Count");
				};
			}
		}

		public BatchLine BatchLine { get; private set; }
		public OrderLine OrderLine { get; set; }

		public virtual string Product
		{
			get
			{
				return OrderLine == null ? BatchLine.ProductSynonym : OrderLine.ProductSynonym;
			}
		}

		public virtual string Producer
		{
			get
			{
				return OrderLine == null ? BatchLine.ProducerSynonym : OrderLine.ProducerSynonym;
			}
		}

		public virtual uint Count
		{
			get
			{
				return OrderLine == null ? BatchLine.Quantity : OrderLine.Count;
			}
		}

		[Style(Description = "Не заказанные")]
		public virtual bool IsNotOrdered
		{
			get { return BatchLine.Status.HasFlag(ItemToOrderStatus.NotOrdered); }
		}

		[Style(Description = "Минимальная цена")]
		public virtual bool IsMinCost
		{
			get { return BatchLine.Status.HasFlag(ItemToOrderStatus.MinimalCost); }
		}

		[Style("Product", Description = "Присутствует в замороженных заказах"), Ignore]
		public virtual bool ExistsInFreezed
		{
			get { return BatchLine.ExistsInFreezed; }
		}
	}

	public class BatchLine : BaseStatelessObject
	{
		private Lazy<Dictionary<string, string>> lazyFields;
		private uint _quantity;

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
			ExportId = (uint)GetHashCode();
			orderLine.ExportBatchLineId = ExportId;
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
			Status = ItemToOrderStatus.NotOrdered;
		}

		public override uint Id { get; set; }

		public virtual Address Address { get; set; }

		public virtual string Code { get; set; }
		public virtual string CodeCr { get; set; }
		public virtual string SupplierDeliveryId { get; set; }
		public virtual string ProductSynonym { get; set; }
		public virtual uint? ProductId { get; set; }
		public virtual uint? CatalogId { get; set; }

		public virtual string ProducerSynonym { get; set; }
		public virtual uint? ProducerId { get; set; }
		public virtual string Producer { get; set; }

		public virtual uint Quantity
		{
			get { return _quantity; }
			set
			{
				_quantity = value;
				OnPropertyChanged();
			}
		}

		public virtual string Comment { get; set;}
		public virtual ItemToOrderStatus Status { get; set; }
		public virtual string ServiceFields { get; set; }
		public virtual uint? ExportId { get; set; }

		public virtual string HasProducerLabel
		{
			get { return ProducerId == null ? "Нет" : "Да"; }
		}

		public virtual string HasOrderLineLabel
		{
			get { return  IsNotOrdered ? "Нет" : "Да"; }
		}

		public virtual Dictionary<string, string> ParsedServiceFields
		{
			get { return lazyFields.Value; }
		}

		[Style(Description = "Не заказанные")]
		public virtual bool IsNotOrdered
		{
			get { return Status.HasFlag(ItemToOrderStatus.NotOrdered); }
		}

		[Style(Description = "Минимальная цена")]
		public virtual bool IsMinCost
		{
			get { return Status.HasFlag(ItemToOrderStatus.MinimalCost); }
		}

		[Style("ProductSynonym", Description = "Присутствует в замороженных заказах"), Ignore]
		public virtual bool ExistsInFreezed { get; set; }

		public override string ToString()
		{
			return String.Format("Comment: {0}, Status: {2}, ProductId: {1}, ProductSynonym: {3}",
				Comment, ProductId, Status, ProductSynonym);
		}

		public static void CalculateStyle(Address[] addresses, IEnumerable<BatchLine> lines)
		{
			var productids = addresses.SelectMany(a => a.Orders).Where(o => o.Frozen)
				.SelectMany(o => o.Lines)
				.ToLookup(l => Tuple.Create(l.Order.Address.Id, l.ProductId));
			foreach (var line in lines) {
				var key = Tuple.Create(line.Address.Id, line.ProductId.GetValueOrDefault());
				line.ExistsInFreezed = productids[key].FirstOrDefault() != null;
			}
		}
	}
}