using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Controls.Behaviors;
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
		NotEnoughLimit = 0x20, //Лимит исчерпан
		SplitByLimit = 0x40, //Разбито из-за лимита
	}

	public class BatchLineView : BaseNotify, IEditableObject, IInlineEditable
	{
		private OrderLine orderLine;

		public BatchLineView(OrderLine line)
		{
			OrderLine = line;
		}

		public BatchLineView(BatchLine batchline, OrderLine orderline)
		{
			BatchLine = batchline;
			OrderLine = orderline;
			BatchLine.PropertyChanged += (sender, args) => {
				if (args.PropertyName == "Quantity")
					OnPropertyChanged("Count");
			};
		}

		public BatchLine BatchLine { get; }

		public OrderLine OrderLine
		{
			get
			{
				return orderLine;
			}
			set
			{
				if (orderLine == value)
					return;
				orderLine = value;
				if (value != null) {
					value.PropertyChanged += (sender, args) => {
						if (args.PropertyName == "Count")
							OnPropertyChanged("Count");
					};
				}
				OnPropertyChanged();
			}
		}

		public virtual string Product => OrderLine?.ProductSynonym ?? BatchLine.ProductSynonym;

		public virtual string Producer => OrderLine?.ProducerSynonym ?? BatchLine.ProducerSynonym;

		//для экспорта
		public virtual uint Count => OrderLine?.Count ?? BatchLine.Quantity;

		[Style(Description = "Не заказанные")]
		public virtual bool IsNotOrdered => BatchLine?.Status.HasFlag(ItemToOrderStatus.NotOrdered) == true;

		[Style(Description = "Минимальная цена")]
		public virtual bool IsMinCost => BatchLine?.Status.HasFlag(ItemToOrderStatus.MinimalCost) == true;

		[Style("Product", Description = "Присутствует в замороженных заказах")]
		public virtual bool ExistsInFreezed { get; set; }

		[Style(Description = "Лимит исчерпан")]
		public virtual bool IsLimited => IsNotOrdered && BatchLine?.Status.HasFlag(ItemToOrderStatus.NotEnoughLimit) == true;

		[Style(Description = "Ограничен лимитом")]
		public virtual bool IsSplitByLimit => !IsNotOrdered && BatchLine?.Status.HasFlag(ItemToOrderStatus.SplitByLimit) == true;

		[Style("BatchLine.Address.Name")]
		public virtual bool IsCurrentAddress { get; set; }

		public Address Address => BatchLine?.Address ?? OrderLine?.Order?.Address;
		public uint? ProductId => BatchLine?.ProductId ?? OrderLine?.ProductId;
		public uint? CatalogId => BatchLine?.CatalogId ?? OrderLine?.CatalogId;

		public string Comment
		{
			get
			{
				if (BatchLine != null)
					return BatchLine.Comment;
				return "Заказано вручную";
			}
		}

		public void BeginEdit()
		{
		}

		public void EndEdit()
		{
		}

		public void CancelEdit()
		{
		}

		public uint Value
		{
			get { return BatchLine?.Quantity ?? 0; }
			set
			{
				if (BatchLine == null)
					return;
				BatchLine.Quantity = value;
			}
		}
	}

	public class BatchLine : BaseStatelessObject, IEditableObject
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

		public virtual string Priority { get; set; }
		public virtual float? BaseCost { get; set; }

		public virtual string Comment { get; set;}
		public virtual ItemToOrderStatus Status { get; set; }
		public virtual string ServiceFields { get; set; }
		public virtual uint? ExportId { get; set; }

		/// <summary>
		/// каталожные свойства товара с которым было произведено сопоставление
		/// </summary>
		public virtual string Properties { get; set; }

		public virtual string HasProducerLabel => ProducerId == null ? "Нет" : "Да";

		public virtual string HasOrderLineLabel => IsNotOrdered ? "Нет" : "Да";

		public virtual Dictionary<string, string> ParsedServiceFields => lazyFields.Value;

		[Style(Description = "Не заказанные")]
		public virtual bool IsNotOrdered => Status.HasFlag(ItemToOrderStatus.NotOrdered);

		[Style(Description = "Минимальная цена")]
		public virtual bool IsMinCost => Status.HasFlag(ItemToOrderStatus.MinimalCost);

		[Style(Description = "Лимит исчерпан")]
		public virtual bool IsLimited => Status.HasFlag(ItemToOrderStatus.NotEnoughLimit);

		[Style(Description = "Ограничен лимитом")]
		public virtual bool IsSplitByLimit => Status.HasFlag(ItemToOrderStatus.SplitByLimit);

		public override string ToString()
		{
			return String.Format("Comment: {0}, Status: {2}, ProductId: {1}, ProductSynonym: {3}",
				Comment, ProductId, Status, ProductSynonym);
		}

		public static void CalculateStyle(Address selectedAddress, Address[] addresses, IEnumerable<BatchLineView> lines)
		{
			var productids = addresses.SelectMany(a => a.Orders).Where(o => o.Frozen)
				.SelectMany(o => o.Lines)
				.ToLookup(l => Tuple.Create(l.Order.Address.Id, l.ProductId));
			foreach (var line in lines) {
				var key = Tuple.Create(line.Address.Id, line.ProductId.GetValueOrDefault());
				line.ExistsInFreezed = productids[key].FirstOrDefault() != null;
				line.IsCurrentAddress = line?.Address.Id == selectedAddress.Id;
			}
		}

		public virtual void BeginEdit()
		{
		}

		public virtual void EndEdit()
		{
		}

		public virtual void CancelEdit()
		{
		}
	}
}