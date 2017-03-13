using System;
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
			if (checkType == CheckType.SaleBuyer)
				Stock.Quantity -= quantity;
			else if (checkType == CheckType.CheckReturn)
				Stock.Quantity += quantity;
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

		[Ignore]
		public virtual Stock Stock { get; set; }

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
				}
			}
		}

		[Ignore, Style]
		public virtual bool Confirmed => ConfirmedQuantity == Quantity;
	}
}
