using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture, Explicit]
	public class ExternalFixture : DbFixture
	{
		[Test]
		public void Load_sbis_doc()
		{
			External.Sbis();
		}
	}
}