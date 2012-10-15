using System.Linq;

namespace AnalitF.Net.Client.Models
{
	public class BaseOffer
	{
		public BaseOffer()
		{
		}

		protected BaseOffer(BaseOffer offer)
		{
			var properties = typeof(BaseOffer).GetProperties().Where(p => p.CanRead && p.CanWrite);
			foreach (var property in properties) {
				var value = property.GetValue(offer, null);
				property.SetValue(this, value, null);
			}
		}

		public virtual uint ProductId { get; set; }

		public virtual uint CatalogId { get; set; }

		public virtual uint ProductSynonymId { get; set; }

		public virtual uint? ProducerId { get; set; }

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

		public virtual uint? RequestRatio { get; set; }

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
	}
}