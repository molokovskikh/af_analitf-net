using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public abstract class BaseOffer : BaseNotify
	{
		private decimal? _retailCost;
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


		public virtual decimal? MinBoundCost { get; set; }

		public virtual decimal? MaxBoundCost { get; set; }


		[Style(Description = "Жизненно важные препараты")]
		public virtual bool VitallyImportant { get; set; }

		public virtual decimal? RegistryCost { get; set; }

		public virtual decimal? MaxProducerCost { get; set; }


		public virtual uint? RequestRatio { get; set; }

		[JsonProperty("OrderCost")]
		public virtual decimal? MinOrderSum { get; set; }

		public virtual uint? MinOrderCount { get; set; }


		public virtual decimal? ProducerCost { get; set; }

		public virtual uint? NDS { get; set; }

		public virtual string EAN13 { get; set; }

		public virtual string CodeOKP { get; set; }

		public virtual string Series { get; set; }

		public virtual string ProductSynonym { get; set; }

		public virtual string ProducerSynonym { get; set; }

		public virtual string BarCode { get; set; }

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
		public virtual uint? SortQuantity
		{
			get { return NullableConvert.ToUInt32(Quantity); }
		}

		[Ignore]
		public virtual decimal? RetailCost
		{
			get { return _retailCost; }
			set
			{
				_retailCost = value;
				OnPropertyChanged();
			}
		}

		public virtual BuyingMatrixStatus BuyingMatrixType { get; set; }

		public virtual void CalculateRetailCost(IEnumerable<MarkupConfig> markups, User user)
		{
			Configure(user);
			var cost =  HideCost ? GetResultCost() : Cost;
			var markup = MarkupConfig.Calculate(markups, this, user);
			RetailCost = Math.Round(cost * (1 + markup / 100), 2);
		}

		public virtual void Configure(User user)
		{
			HideCost = user.IsDelayOfPaymentEnabled && !user.ShowSupplierCost;
		}

		protected virtual decimal GetResultCost(Price price)
		{
			if (price == null)
				return Cost;

			return Math.Round(VitallyImportant ? Cost * price.VitallyImportantCostFactor : Cost * price.CostFactor, 2);
		}

		protected virtual decimal GetResultCost(Price price, decimal? cost)
		{
			if (price == null || cost == null)
				return Cost;

			return Math.Round(cost.Value * (VitallyImportant ? price.VitallyImportantCostFactor : price.CostFactor), 2);
		}

		public abstract decimal GetResultCost();
	}
}