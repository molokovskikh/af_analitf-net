using System;
using System.IO;
using NUnit.Framework;

namespace AnalitF.Net.Service.Test.Unit
{
	[TestFixture]
	public class MySqlHelperFixture
	{
		[Test]
		public void Export()
		{
			var writer = new StringWriter(formatProvider: null);
			var a = new[] { new object[] { new DateTime(2013, 5, 1, 10, 0, 0), 1.2 } };
			Common.MySql.MySqlHelper.Export(a, writer);
			Assert.AreEqual("2013-05-01 10:00:00\t1.2\r\n", writer.ToString());
		}

		[Test]
		public void Export_new_line()
		{
			var writer = new StringWriter(formatProvider: null);
			var a = new[] { new object[] { "1\r\n2", 1.2 } };
			Common.MySql.MySqlHelper.Export(a, writer);
			Assert.AreEqual("1\r\\\n2\t1.2\r\n", writer.ToString());}
	}
}