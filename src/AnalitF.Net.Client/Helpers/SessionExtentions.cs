using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using NHibernate;

namespace AnalitF.Net.Client.Helpers
{
	public static class SessionExtentions
	{
		public static void DeleteEach<T>(this ISession session, IEnumerable<T> items)
		{
			items.ToList().Each(i => session.Delete(i));
		}
	}
}