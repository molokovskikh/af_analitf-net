using System.Linq;
using AnalitF.Net.Client.Models;
using Common.NHibernate;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class SimpleFixture
	{
		public static void CreateCleanAwaited(ISession session)
		{
			session.DeleteEach<AwaitedItem>();
			var offer = session.Query<Offer>().First();
			var catalog = session.Load<Catalog>(offer.CatalogId);
			var item = new AwaitedItem(catalog);
			session.Save(item);
		}
	}
}