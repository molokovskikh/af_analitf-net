using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class SanityCheckFixture
	{
		[Test]
		public void Make_check()
		{
			var check = new SanityCheck();
			check.Check();
		}
	}
}