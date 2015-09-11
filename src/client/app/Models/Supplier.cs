using NHibernate;
using Test.Support.Suppliers;

namespace AnalitF.Net.Client.Models
{
	public class Supplier
	{
		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual string FullName { get; set; }

		public virtual bool HaveCertificates { get; set; }

		public static void InvalidateCache(TestSupplier supplier, User user, ISession session)
		{
			session.CreateSQLQuery(@"update Usersettings.AnalitFReplicationInfo
set ForceReplication = 1
where userId = :userId and FirmCode = :supplierId")
				.SetParameter("supplierId", supplier.Id)
				.SetParameter("userId", user.Id)
				.ExecuteUpdate();
		}
	}
}