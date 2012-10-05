using System.Linq;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models
{
	public class SanityCheck
	{
		public void Check()
		{
			using (var session = Config.Initializers.NHibernate.Factory.OpenSession()) {
				var settings = session.Query<Settings>().FirstOrDefault();
				if (settings == null) {
					session.Save(new Settings());
				}
			}
		}
	}
}