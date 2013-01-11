namespace AnalitF.Net.Client.Models
{
	public class Catalog
	{
		public Catalog()
		{
		}

		public Catalog(string name)
		{
			Name = new CatalogName { Name = name };
		}

		public virtual uint Id { get; set; }

		public virtual uint FormId { get; set; }

		public virtual string Form { get; set; }

		public virtual CatalogName Name { get; set; }

		public virtual bool VitallyImportant { get; set; }

		public virtual bool MandatoryList { get; set; }

		public virtual bool HaveOffers { get; set; }

		public virtual string FullName
		{
			get { return Name.Name + " " + Form; }
		}

		public override string ToString()
		{
			return FullName;
		}
	}
}