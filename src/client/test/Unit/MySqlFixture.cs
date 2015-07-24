using System;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class MySqlFixture
	{
		[Test]
		public void Fix_connection_string()
		{
			var value = "User Id=root; Server Parameters=\"--basedir=.;--datadir=.;--innodb=OFF\"; Embedded=True; Database=data";
			var nhibernate = new Client.Config.NHibernate.NHibernate();
			var result = nhibernate.FixRelativePaths(value);
			Assert.That(result, Is.StringContaining("--basedir=" + Environment.CurrentDirectory.Replace("\\", "/")));
		}
	}
}