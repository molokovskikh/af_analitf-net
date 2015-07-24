using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class AddAwaitedFixture : ViewModelFixture<AddAwaited>
	{
		[Test]
		public void Search_items()
		{
			model.CatalogTerm.Value = session.Query<Catalog>().First().FullName.Substring(0, 3);
			scheduler.AdvanceByMs(500);
			Assert.That(model.Catalogs.Value.Count, Is.GreaterThan(0));

			model.ProducerTerm.Value = session.Query<Producer>().First().Name.Substring(0, 3);
			scheduler.AdvanceByMs(500);
			//в списке всегда будет Все производители
			Assert.That(model.Producers.Value.Count, Is.GreaterThan(1));
		}
	}
}