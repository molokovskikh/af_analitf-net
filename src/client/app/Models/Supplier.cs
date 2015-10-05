using NHibernate;

namespace AnalitF.Net.Client.Models
{
	public class Supplier
	{
		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual string FullName { get; set; }

		public virtual string DiadokOrgId { get; set; }

		public virtual bool HaveCertificates { get; set; }
		/// <summary>
		/// идентификатор поставщика для отчета
		/// </summary>
		public virtual string VendorId { get; set; }
	}
}
