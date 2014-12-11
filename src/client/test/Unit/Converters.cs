using System;
using System.Globalization;
using AnalitF.Net.Client.Extentions;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class Converters
	{
		[Test]
		public void Convert()
		{
			var c = new NullableConverter();
			Assert.IsNull(c.ConvertBack("", typeof(int?), null, CultureInfo.CurrentCulture));
			Assert.AreEqual(1, c.ConvertBack("1", typeof(int?), null, CultureInfo.CurrentCulture));

			Assert.AreEqual("", c.Convert(null, typeof(string), null, CultureInfo.CurrentCulture));
			Assert.AreEqual("1", c.Convert(1, typeof(string), null, CultureInfo.CurrentCulture));
		}
	}
}