using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Helpers
{
	public static class SessionExtentions
	{
		public static void DeleteEach<T>(this ISession session)
		{
			DeleteEach(session, session.Query<T>());
		}

		public static void DeleteEach<T>(this ISession session, IEnumerable<T> items)
		{
			items.ToList().Each(i => session.Delete(i));
		}

		public static void SaveEach<T>(this ISession session, IEnumerable<T> items)
		{
			items.ToList().Each(i => session.Save(i));
		}

		public static void SaveEach<T>(this ISession session, params T[] items)
		{
			items.Each(i => session.Save(i));
		}
	}
}