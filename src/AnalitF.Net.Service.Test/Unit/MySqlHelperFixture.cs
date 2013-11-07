using System;
using System.IO;
using AnalitF.Net.Service.Helpers;
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
			MySqlHelper.Export(a, writer);
			Assert.AreEqual("2013-05-01 10:00:00\t1.2\t\r\n", writer.ToString());
		}
	}
}