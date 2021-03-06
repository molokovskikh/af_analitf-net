﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class CheckLine : BaseStock, IInlineEditable
	{
		private decimal _quantity;
		private uint _confirmedQuantity;

		public CheckLine()
		{

		}

		public CheckLine(uint id)
		{
			CheckId = id;
		}

		public CheckLine(Stock stock, uint quantity, CheckType checkType)
		{
			WaybillLineId = stock.WaybillLineId;
			if (checkType == CheckType.SaleBuyer && stock.Quantity < quantity)
				throw new Exception($"У позиции {stock.Product} нет достаточного количества, требуется {quantity} в наличии {stock.Quantity}");
			Stock = stock;
			//if (checkType == CheckType.SaleBuyer)
			//	Stock.Quantity -= quantity;
			//else if (checkType == CheckType.CheckReturn)
			//	Stock.Quantity += quantity;
			CopyFromStock(stock);
			Quantity = quantity;
		}

		public CheckLine(Stock stock, uint quantity)
		{
			WaybillLineId = stock.WaybillLineId;
			if (stock.Quantity < quantity)
				throw new Exception($"У позиции {stock.Product} нет достаточного количества, требуется {quantity} в наличии {stock.Quantity}");
			Stock = stock;
			CopyFromStock(stock);
			Quantity = quantity;
		}

		public CheckLine(Stock stock, Stock sourceStock, uint quantity)
		{
			WaybillLineId = stock.WaybillLineId;
			if (stock.Quantity < quantity)
				throw new Exception($"У позиции {stock.Product} нет достаточного количества, требуется {quantity} в наличии {stock.Quantity}");
			Stock = stock;
			SourceStock = sourceStock;
			CopyFromStock(stock);
			Quantity = quantity;
		}

		public CheckLine(BarcodeProducts barcodeProduct, uint quantity, decimal retailCost)
		{
			BarcodeProduct = barcodeProduct;
			Barcode = barcodeProduct.Barcode;
			Product = barcodeProduct.Product.Name;
			ProductId = barcodeProduct.Product.Id;
			CatalogId = barcodeProduct.Product.CatalogId;
			Producer = barcodeProduct.Producer.Name;
			ProducerId = barcodeProduct.Producer.Id;
			Quantity = quantity;
			RetailCost = retailCost;
		}

		public virtual BarcodeProducts BarcodeProduct { get; set; }

		public virtual Check Doc { get; set; }

		public virtual uint Id { get; set; }
		public virtual uint? WaybillLineId { get; set; }
		public virtual uint? ServerDocId { get; set; }

		public virtual decimal Quantity
		{
			get { return _quantity; }
			set
			{
				if (_quantity != value) {
					_quantity = value;
					ConfirmedQuantity = 0;
					OnPropertyChanged();
					OnPropertyChanged(nameof(RetailSum));
				}
			}
		}

		public virtual decimal RetailSum => Quantity * RetailCost.GetValueOrDefault();
		public virtual decimal Sum => RetailSum - DiscontSum;
		public virtual decimal DiscontSum  { get; set; }
		public virtual uint? CheckId { get; set; }
		public virtual uint? ProductKind { get; set; }
		public virtual string PKU
		{
			get
			{
				if (Narcotic)
					return "ПКУ:Наркотические и психотропные";
				if (Toxic)
					return "ПКУ:Сильнодействующие. и ядовитые";
				if (Combined)
					return "ПКУ:Комбинированные";
				if (Other)
					return "ПКУ:Иные лек.средства";
				return null;
			}
		}
		public virtual uint? Divider { get; set; }
		public virtual decimal MarkupSum { get; set; }
		public virtual decimal NDSSum { get; set; }
		public virtual decimal NPSum { get; set; }
		public virtual uint? NDS { get; set; }
		public virtual uint? NP { get; set; }
		public virtual decimal PartyNumber { get; set; }

		public virtual bool Narcotic { get; set; }
		public virtual bool Toxic { get; set; }
		public virtual bool Combined { get; set; }
		public virtual bool Other { get; set; }
		public virtual bool IsPKU => Narcotic || Toxic || Combined || Other;

		public virtual Stock Stock { get; set; }

		[Ignore]
		public virtual Stock SourceStock { get; set; }

		public virtual void CopyToStock(Stock stock)
		{
			Copy(this, stock);
		}

		public virtual void CopyFromStock(Stock stock)
		{
			Copy(stock, this);
		}

		private static void Copy(object srcItem, object dstItem)
		{
			var srcProps = srcItem.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite);
			var dstProps = dstItem.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite).ToDictionary(x => x.Name);
			foreach (var srcProp in srcProps) {
				var dstProp = dstProps.GetValueOrDefault(srcProp.Name);
				dstProp?.SetValue(dstItem, srcProp.GetValue(srcItem, null), null);
			}
		}

		[Ignore]
		public virtual uint Value
		{
			get { return (uint)Quantity; }
			set { Quantity = value; }
		}

		[Ignore]
		public virtual uint ConfirmedQuantity
		{
			get { return _confirmedQuantity; }
			set
			{
				if (_confirmedQuantity != value) {
					_confirmedQuantity = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(Confirmed));
					OnPropertyChanged(nameof(NotConfirmed));
				}
			}
		}

		[Ignore, Style(Description = "Подтверждена")]
		public virtual bool Confirmed => ConfirmedQuantity == Quantity;

		[Ignore, Style(Description = "Не подтверждена")]
		public virtual bool NotConfirmed => ConfirmedQuantity != Quantity;

		public virtual StockAction UpdateStock(Stock stock, CheckType checkType)
		{
			if (checkType == CheckType.SaleBuyer)
			{
				stock.Quantity -= Quantity;
				return new StockAction(ActionType.Sale, ActionTypeChange.Minus, stock, Doc, Quantity, DiscontSum);
			}
			else
			{
				stock.Quantity += Quantity;
				return new StockAction(ActionType.CheckReturn, ActionTypeChange.Plus, stock, Doc, Quantity, DiscontSum);
			}
		}
	}
}
