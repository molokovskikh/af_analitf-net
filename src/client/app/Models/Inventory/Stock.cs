using System;
using System.Linq;
using System.Collections.Generic;
using AnalitF.Net.Client.Helpers;
using System.ComponentModel;
using AnalitF.Net.Client.Config.NHibernate;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models.Inventory
{
	public enum StockStatus
	{
		[Description("Доступен")] Available,
		[Description("В пути")] InTransit,
	}

	public enum RejectStatus
	{
		[Description("Неизвестно")]
		Unknown,
		[Description("Возможно")]
		Perhaps,
		[Description("Брак")]
		Defective,
		[Description("Нет")]
		NotDefective,
	}

	public class BaseStock : BaseNotify
	{
		public virtual string Barcode { get; set; }
		public virtual string Product { get; set; }
		public virtual uint? ProductId { get; set; }
		public virtual uint? CatalogId { get; set; }
		public virtual string Producer { get; set; }
		public virtual uint? ProducerId { get; set; }
		public virtual string SerialNumber { get; set; }
		public virtual string Certificates { get; set; }
		public virtual decimal? ProducerCost { get; set; }
		public virtual decimal? RegistryCost { get; set; }
		public virtual decimal? RetailCost { get; set; }
		public virtual decimal? RetailMarkup { get; set; }

		public virtual decimal? SupplierCost { get; set; }
		public virtual decimal? SupplierCostWithoutNds { get; set; }
		public virtual decimal? SupplierPriceMarkup { get; set; }

		public virtual decimal? ExciseTax { get; set; }
		public virtual string BillOfEntryNumber { get; set; }
		public virtual bool? VitallyImportant { get; set; }

		public virtual decimal SupplyQuantity { get; set; }
	}

	public class Stock : BaseStock
	{
		private decimal? _retailCost;
		private decimal? _retailMarkup;

		public Stock()
		{
		}

		public Stock(ReceivingOrder order, ReceivingLine line)
		{
			WaybillId = order.WaybillId;
			Status = StockStatus.Available;
			Address = order.Address;
			line.CopyToStock(this);
		}

		public virtual uint Id { get; set; }

		public virtual ulong? ServerId { get; set; }
		public virtual int? ServerVersion { get; set; }

		public virtual Address Address { get; set; }
		public virtual StockStatus Status { get; set; }

		public virtual uint? ReceivingOrderId { get; set; }
		public virtual uint? WaybillId { get; set; }

		public virtual string AnalogCode { get; set; }
		public virtual string ProducerBarcode { get; set; }
		public virtual string AltBarcode { get; set; }
		public virtual string AnalogGroup { get; set; }
		public virtual string Country { get; set; }
		public virtual string Unit { get; set; }

		public virtual string ProductKind { get; set; }

		public virtual string FarmGroup { get; set; }
		public virtual string Mnn { get; set; }
		public virtual string Brand { get; set; }
		public virtual string UserCategory { get; set; }
		public virtual string Category { get; set; }
		public virtual string RegionCert { get; set; }
		public virtual decimal Quantity { get; set; }
		public virtual decimal ReservedQuantity { get; set; }

		public virtual int? Nds { get; set; }
		public virtual decimal? NdsAmount { get; set; }
		public virtual double NdsPers { get; set; }
		public virtual double NpPers { get; set; }
		public virtual decimal Excise { get; set; }

		public virtual string StatusName => DescriptionHelper.GetDescription(Status);

		[Ignore]
		public virtual Settings Settings { get; set; }

		[Ignore]
		public virtual WaybillSettings WaybillSettings { get; set; }

		[Ignore]
		public virtual bool SpecialMarkup { get; set; }

		public override decimal? RetailCost
		{
			get { return _retailCost; }
			set
			{
				if (_retailCost == value)
					return;
				_retailCost = value;
				RecalculateMarkups(RetailCost);
				OnPropertyChanged();
				OnPropertyChanged(nameof(RetailSum));
			}
		}

		public override decimal? RetailMarkup
		{
			get { return _retailMarkup; }
			set
			{
				if (_retailMarkup == value)
					return;
				_retailMarkup = value;
				RecalculateFromRetailMarkup();
				OnPropertyChanged();
			}
		}

		private void RecalculateMarkups(decimal? retailsCost)
		{
			if (Settings == null)
				return;
			UpdateMarkups(retailsCost);
			OnPropertyChanged(nameof(RetailMarkup));
		}

		private void RecalculateFromRetailMarkup()
		{
			if (Settings == null)
				return;
			_retailCost = CalculateRetailCost(_retailMarkup);
			OnPropertyChanged(nameof(RetailCost));
			OnPropertyChanged(nameof(RetailSum));
		}

		private void UpdateMarkups(decimal? cost)
		{
			if (!IsCalculable())
				return;

			if (GetMarkupType() == MarkupType.VitallyImportant)
				_retailMarkup = NullableHelper.Round((cost - SupplierCost) / (ProducerCost * TaxFactor) * 100, 2);
			else
				_retailMarkup = NullableHelper.Round((cost - SupplierCost) / (SupplierCostWithoutNds * TaxFactor) * 100, 2);
		}

		private bool IsCalculable()
		{
			if (SupplierCost.GetValueOrDefault() == 0)
				return false;
			if (GetMarkupType() == MarkupType.VitallyImportant) {
				if (ProducerCost.GetValueOrDefault() == 0)
					return false;
			} else if (SupplierCostWithoutNds.GetValueOrDefault() == 0) {
				return false;
			}
			return true;
		}

		private decimal TaxFactor
		{
			get
			{
				var nds = Nds.GetValueOrDefault(10);
				if (WaybillSettings.Taxation == Taxation.Envd
					&& ((VitallyImportant.GetValueOrDefault() && !WaybillSettings.IncludeNdsForVitallyImportant)
						|| !VitallyImportant.GetValueOrDefault() && !WaybillSettings.IncludeNds)) {
					nds = 0;
				}
				return (1 + nds / 100m);
			}
		}

		private MarkupType GetMarkupType()
		{
			var markupType = VitallyImportant.GetValueOrDefault() ? MarkupType.VitallyImportant : MarkupType.Over;
			if (Nds == 18 && (markupType != MarkupType.VitallyImportant || ProducerCost.GetValueOrDefault() == 0))
				markupType = MarkupType.Nds18;
			if (SpecialMarkup)
				markupType = MarkupType.Special;
			return markupType;
		}

		private decimal? CalculateRetailCost(decimal? markup)
		{
			var type = GetMarkupType();
			var baseCost = SupplierCostWithoutNds;
			if (type == MarkupType.VitallyImportant)
				baseCost = ProducerCost;

			var value = SupplierCost + baseCost * markup / 100 * TaxFactor;
			return Round(NullableHelper.Round(value, 2));
		}

		private decimal? Round(decimal? value)
		{
			if (Settings.Rounding != Rounding.None) {
				var @base = 10;
				var factor = 1;
				if (Settings.Rounding == Rounding.To1_00) {
					@base = 1;
				}
				else if (Settings.Rounding == Rounding.To0_50) {
					@factor = 5;
				}
				var normalized = (int?)(value * @base);
				return (normalized - normalized % factor) / (decimal)@base;
			}
			return value;
		}


		public virtual decimal? LowCost { get; set; }
		public virtual decimal? LowMarkup
		{
			get
			{
				if (RetailCost != 0 && LowCost != null)
					return Math.Round(((LowCost - SupplierCost) * 100 / SupplierCost).Value, 2);
				return null;
			}
		}

		public virtual decimal? OptCost { get; set; }
		public virtual decimal? OptMarkup
		{
			get
			{
				if (SupplierCost != 0 && OptCost != null)
					return Math.Round((((OptCost - SupplierCost) * 100) / SupplierCost).Value, 2);
				return null;
			}
		}

		public virtual decimal SupplySum => SupplyQuantity * SupplierCost.GetValueOrDefault();
		public virtual decimal SupplySumWithoutNds => SupplyQuantity * SupplierCostWithoutNds.GetValueOrDefault();
		public virtual decimal? RetailSum => Quantity * RetailCost;
		public virtual string Vmn { get; set; }
		public virtual string Gtd { get; set; }
		public virtual DateTime? Exp { get; set; }
		public virtual string Period { get; set; }
		public virtual string DocumentDate { get; set; }
		public virtual string WaybillNumber { get; set; }

		public virtual RejectStatus RejectStatus { get; set; }

		public virtual string RejectStatusName => DescriptionHelper.GetDescription(RejectStatus);

		public static IQueryable<Stock> AvailableStocks(IStatelessSession session, Address address = null)
		{
			var query = session.Query<Stock>().Where(x => x.Quantity > 0 && x.Status == StockStatus.Available);
			if (address != null)
				query = query.Where(x => x.Address == address);
			return query;
		}

		// продажа, из резерва наружу
		public virtual StockAction ApplyReserved(decimal quantity)
		{
			ReservedQuantity -= quantity;
			return new StockAction(ActionType.Sale, this, quantity);
		}

		// возврат поставщику, из резерва наружу
		public virtual StockAction ReturnToSupplier(decimal quantity)
		{
			ReservedQuantity -= quantity;
			return new StockAction(ActionType.ReturnToSupplier, this, quantity);
		}

		// отмена возврат поставщику, снаружи в резерв
		public virtual StockAction CancelReturnToSupplier(decimal quantity)
		{
			ReservedQuantity += quantity;
			return new StockAction(ActionType.CancelReturnToSupplier, this, quantity);
		}

		// инвентаризация, снаружи в поставку
		public virtual StockAction InventoryDoc(decimal quantity)
		{
			SupplyQuantity += quantity;
			return new StockAction(ActionType.InventoryDoc, this, quantity);
		}

		// отмена инвентаризации, с поставки наружу
		public virtual StockAction CancelInventoryDoc(decimal quantity)
		{
			SupplyQuantity -= quantity;
			return new StockAction(ActionType.CancelInventoryDoc, this, quantity);
		}

		// Перемещение между складами. С резерва наружу, на др склад
		public virtual StockAction DisplacementTo(decimal quantity)
		{
			ReservedQuantity -= quantity;
			return new StockAction(ActionType.DisplacementTo, this, quantity);
		}

		// отмена перемещения между складами. Снаружи с др. склада в резерв
		public virtual StockAction CancelDisplacementTo(decimal quantity)
		{
			ReservedQuantity += quantity;
			return new StockAction(ActionType.CancelDisplacementTo, this, quantity);
		}

		// Перемещение между складами. Снаружи с др. склада в поставку
		public virtual StockAction DisplacementFrom(decimal quantity)
		{
			SupplyQuantity += quantity;
			return new StockAction(ActionType.DisplacementFrom, this, quantity);
		}

		// отмена перемещения между складами. С поставки наружу на др. склад
		public virtual StockAction CancelDisplacementFrom(decimal quantity)
		{
			SupplyQuantity -= quantity;
			return new StockAction(ActionType.CancelDisplacementFrom, this, quantity);
		}

		// из резерва на склад
		public virtual void Release(decimal quantity)
		{
			ReservedQuantity -= quantity;
			Quantity += quantity;
		}

		// со склада в резерв
		public virtual void Reserve(decimal quantity)
		{
			Quantity -= quantity;
			ReservedQuantity += quantity;
		}

		// с поставки на склад
		public virtual void Incoming(decimal quantity)
		{
			SupplyQuantity -= quantity;
			Quantity += quantity;
		}

		// со склада в поставку
		public virtual void CancelIncoming(decimal quantity)
		{
			SupplyQuantity += quantity;
			Quantity -= quantity;
		}

		public virtual Stock Copy()
		{
			var item = (Stock)MemberwiseClone();
			item.Id = 0;
			item.ServerId = null;
			item.ServerVersion = null;
			ReservedQuantity = 0;
			return item;
		}

		public virtual void Configure(Settings settings)
		{
			Settings = settings;
			WaybillSettings = settings.Waybills.First(x => x.BelongsToAddress.Id == Address.Id);
		}
	}
}