using System.IO;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture, Ignore]
	public class SanityCheckFixture
	{
		[Test]
		public void Make_check()
		{
			var check = new SanityCheck("data");
			check.Check();
		}

		[Test]
		public void Create_local_db()
		{
			var dataPath = "data";
			if (Directory.Exists(dataPath))
				Directory.Delete(dataPath, true);

			var sanityCheck = new SanityCheck(dataPath);
			sanityCheck.Check();

			Assert.That(Directory.Exists(dataPath));
		}
	}
}