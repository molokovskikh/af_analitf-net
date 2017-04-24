using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Newtonsoft.Json;
using AnalitF.Net.Client.Config;

namespace AnalitF.Net.Client.Models
{
	public abstract class BaseOffer : BaseNotify
	{
		protected bool HideCost;

		public BaseOffer()
		{
		}

		protected BaseOffer(BaseOffer offer)
		{
			Clone(offer);
		}

		public virtual void Clone(BaseOffer offer)
		{
			var properties = typeof(BaseOffer).GetProperties().Where(p => p.CanRead && p.CanWrite);
			foreach (var property in properties) {
				var value = property.GetValue(offer, null);
				property.SetValue(this, value, null);
			}
		}

		public virtual uint ProductId { get; set; }

		public virtual uint CatalogId { get; set; }

		public virtual uint? CategoryId { get; set; }

		[JsonProperty("SynonymCode")]
		public virtual uint ProductSynonymId { get; set; }

		public virtual string Producer { get; set; }

		public virtual uint? ProducerId { get; set; }

		[JsonProperty("SynonymFirmCrCode")]
		public virtual uint? ProducerSynonymId { get; set; }

		public virtual string Code { get; set; }

		public virtual string CodeCr { get; set; }

		public virtual string Unit { get; set; }

		public virtual string Volume { get; set; }

		public virtual string Quantity { get; set; }

		public virtual string Note { get; set; }

		public virtual string Period { get; set; }

		public virtual DateTime? Exp { get; set; }

		public virtual string Doc { get; set; }

		//в предложениях мы отображаем ResultCost в заказах MixedCost
		[Style("Period", "ResultCost", "MixedCost", Description = "Уцененные препараты")]
		public virtual bool Junk { get; set; }

		/// <summary>
		/// Junk - флаг уценки с учетом клиентских настроек
		/// OriginalJunk - флаг уценки без учета клиентских настроек
		/// </summary>
		public virtual bool OriginalJunk { get; set; }


		public virtual decimal? MinBoundCost { get; set; }

		public virtual decimal? MaxBoundCost { get; set; }


		[Style(Description = "Жизненно важные препараты")]
		public virtual bool VitallyImportant { get; set; }

		public virtual decimal? RegistryCost { get; set; }

		public virtual decimal? MaxProducerCost { get; set; }

		public virtual uint? RequestRatio { get; set; }

		public virtual uint SafeRequestRatio
		{
			get
			{
				if (RequestRatio == 0)
					return 1;
				return RequestRatio.GetValueOrDefault(1);
			}
		}

		[JsonProperty("OrderCost")]
		public virtual decimal? MinOrderSum { get; set; }

		public virtual uint? MinOrderCount { get; set; }


		public virtual decimal? ProducerCost { get; set; }

		public virtual uint? NDS { get; set; }

		public virtual string CodeOKP { get; set; }

		public virtual string Series { get; set; }

		public virtual string ProductSynonym { get; set; }

		public virtual string ProducerSynonym { get; set; }

		public virtual string BarCode { get; set; }

		public virtual string Properties { get; set; }

		/// <summary>
		/// цена поставщика
		/// </summary>
		public virtual decimal Cost { get; set; }

		[IgnoreDataMember]
		public virtual decimal? SupplierMarkup
		{
			get
			{
				if (ProducerCost == 0)
					return null;
				var nds = NDS ?? 10.0m;
				return NullableHelper.Round((Cost / (ProducerCost * (nds / 100 + 1)) - 1) * 100, 2);
			}
		}
		//поля для сортировки
		[IgnoreDataMember]
		public virtual uint? SortQuantity => NullableConvert.ToUInt32(Quantity);

		public virtual BuyingMatrixStatus BuyingMatrixType { get; set; }

		[Ignore]
		public virtual bool IsSpecialMarkup { get; set; }

		[Ignore]
		public virtual WaybillSettings WaybillSettings { get; set; }
		private decimal TaxFactor =>
			WaybillSettings.Taxation == Taxation.Envd
					&& (VitallyImportant && !WaybillSettings.IncludeNdsForVitallyImportant
						|| !VitallyImportant && !WaybillSettings.IncludeNds)
				? 1m
				: (1 + NDS.GetValueOrDefault(10) / 100m);

		public virtual MarkupType GetMarkupType()
		{
			if (IsSpecialMarkup)
				return MarkupType.Special;
			var markupType = VitallyImportant ? MarkupType.VitallyImportant : MarkupType.Over;
			if (NDS == 18 && (markupType != MarkupType.VitallyImportant || ProducerCost.GetValueOrDefault() == 0))
				markupType = MarkupType.Nds18;
			return markupType;
		}

		public virtual void Configure(User user)
		{
			HideCost = user.IsDelayOfPaymentEnabled && !user.ShowSupplierCost;
		}

		protected virtual decimal GetResultCost(Price price)
		{
			if (price == null)
				return Cost;

			var factor = price.CostFactor;
			if (VitallyImportant)
				factor = price.VitallyImportantCostFactor;
			else if (CategoryId == 1)
				factor = price.SupplementCostFactor;
			return Math.Round(Cost * factor, 2);
		}

		protected virtual decimal GetResultCost(Price price, decimal? cost)
		{
			if (price == null || cost == null)
				return Cost;
			var factor = price.CostFactor;
			if (VitallyImportant)
				factor = price.VitallyImportantCostFactor;
			else if (CategoryId == 1)
				factor = price.SupplementCostFactor;
			return Math.Round(cost.Value * factor, 2);
		}

		public abstract decimal GetResultCost();

		private Settings _settings;

		[Ignore]
		public virtual Settings Settings
		{
			get
			{
				if (_settings == null)
					_settings = Env.Current?.Settings;
				return _settings;
			}
			set { _settings = value; }
		}

		protected decimal GetRetailCost(decimal markup, Address address)
		{
			WaybillSettings = Settings.Waybills.FirstOrDefault(r => r.BelongsToAddress == address);
			var type = GetMarkupType();
			var cost = HideCost ? GetResultCost() : Cost;
			var baseCost = type == MarkupType.VitallyImportant && ProducerCost.HasValue ? ProducerCost.Value : cost;
			var taxFactor = type == MarkupType.VitallyImportant && ProducerCost.HasValue && !HideCost ? TaxFactor : 1m;
			return Util.Round(Math.Round(cost + baseCost * markup / 100 * taxFactor, 2), Settings.Rounding) ?? 0;
		}

		[Style("RetailCost", Description = "Расчёт от цены поставщика")]
		public virtual bool IsVIPriceCalcFromSupplier => VitallyImportant && !ProducerCost.HasValue;
		[Style("RetailCost", Description = "Расчёт от цены производителя")]
		public virtual bool IsVIPriceCalcFromProducer => VitallyImportant && ProducerCost.HasValue;
	}
}