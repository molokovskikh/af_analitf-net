namespace AnalitF.Net.Client.Models
{
	public class Catalog
	{
		public virtual uint Id { get; set; }

		public virtual uint FormId { get; set; }

		public virtual string Form { get; set; }

		public virtual CatalogName Name { get; set; }

		public virtual bool VitallyImportant { get; set; }

		public virtual bool MandatoryList { get; set; }

		public virtual bool HaveOffers { get; set; }

		public virtual string Fullname
		{
			get { return Name.Name + " " + Form; }
		}
	}
}