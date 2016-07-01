using AnalitF.Net.Service.Test.TestHelpers;
using NHibernate;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateNews : ServerFixture
	{
		public override void Execute(ISession session)
		{
			DataMother.CreateNews(session);
		}
	}
}