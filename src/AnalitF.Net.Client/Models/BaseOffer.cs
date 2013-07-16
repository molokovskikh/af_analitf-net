using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Config.Initializers;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Models
{
	public class BaseOffer
	{
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

		public virtual string Doc { get; set; }

		public virtual bool Junk { get; set; }


		public virtual decimal? MinBoundCost { get; set; }

		public virtual decimal? MaxBoundCost { get; set; }


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

		public virtual decimal Cost { get; set; }

		public virtual decimal? SupplierCost
		{
			get { return Cost; }
		}

		public virtual decimal? SupplierMarkup
		{
			get
			{
				if (ProducerCost == null)
					return null;
				var nds = NDS ?? 10.0m;
				return Math.Round((Cost / (ProducerCost.Value * (nds / 100 + 1)) - 1) * 100, 2);
			}
		}

		[Ignore]
		public virtual decimal RetailCost { get; set; }

		public virtual void CalculateRetailCost(IList<MarkupConfig> markups)
		{
			var markup = MarkupConfig.Calculate(markups, this);
			RetailCost = Math.Round(Cost * (1 + markup / 100), 2);
		}
	}
}