namespace AnalitF.Net.Client.Models
{
	public class Catalog
	{
		public virtual uint Id { get; set; }

		public virtual CatalogForm Form { get; set; }

		public virtual CatalogName Name { get; set; }
	}
}