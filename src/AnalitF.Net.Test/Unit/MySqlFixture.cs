using System;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class MySqlFixture
	{
		[Test]
		public void Fix_connection_string()
		{
			var value = "User Id=root; Server Parameters=\"--basedir=.;--datadir=.;--innodb=OFF\"; Embedded=True; Database=data";
			var result = Client.Config.Initializers.NHibernate.FixRelativePaths(value);
			Assert.That(result, Is.StringContaining("--basedir=" + Environment.CurrentDirectory.Replace("\\", "/")));
		}
	}
}