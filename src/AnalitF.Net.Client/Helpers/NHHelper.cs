using Devart.Common;
using NHibernate;
using NHibernate.Engine;

namespace AnalitF.Net.Client.Helpers
{
	public class NHHelper
	{
		public static EntityEntry Reassociate(ISession session, object entity, EntityEntry entry)
		{
			var context = session.GetSessionImplementation().PersistenceContext;
			return context.AddEntity(entity,
				entry.Status,
				entry.LoadedState,
				entry.EntityKey,
				entry.Version,
				entry.LockMode,
				entry.ExistsInDatabase,
				entry.Persister,
				entry.IsBeingReplicated,
				entry.LoadedWithLazyPropertiesUnfetched);
		}
	}
}