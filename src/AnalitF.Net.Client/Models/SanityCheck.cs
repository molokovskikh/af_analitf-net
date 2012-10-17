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

				var markups = session.Query<MarkupConfig>().ToList();
				if (markups.Count == 0) {
					var defaults = MarkupConfig.Defaults();
					foreach (var markup in defaults) {
						session.Save(markup);
					}
				}
			}
		}
	}
}