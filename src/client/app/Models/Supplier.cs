namespace AnalitF.Net.Client.Models
{
	public class Supplier
	{
		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual string FullName { get; set; }

		public virtual bool HaveCertificates { get; set; }
	}
}