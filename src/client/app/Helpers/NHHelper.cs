using System;
using System.Collections.Generic;
using System.Linq;
using Devart.Common;
using NHibernate;
using NHibernate.Engine;

namespace AnalitF.Net.Client.Helpers
{
	public static class NHHelper
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

		public static bool IsExists(Func<bool> check)
		{
			bool notFound;
			try {
				notFound = check();
			}
			catch (LazyInitializationException) {
				notFound = true;
			}
			catch (ObjectNotFoundException) {
				notFound = true;
			}
			catch (SessionException) {
				notFound = true;
			}
			return !notFound;
		}

		public static void InsertEach<T>(this IStatelessSession session, IEnumerable<T> items)
		{
			var toSave = items.ToArray();
			foreach (var item in toSave) {
				session.Insert(item);
			}
		}
	}
}