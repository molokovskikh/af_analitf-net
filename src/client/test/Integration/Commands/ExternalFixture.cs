using System.Threading.Tasks;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture, Explicit]
	public class ExternalFixture : DbFixture
	{
		[Test]
		public async Task Load_sbis_doc()
		{
			await External.Sbis();
		}
	}
}