using AnalitF.Net.Client.Helpers;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class NotifyValueFixture
	{
		[Test]
		public void Dependend_property()
		{
			var p1 = new NotifyValue<int>(1);
			var p2 = new NotifyValue<int>(() => p1.Value + 1, p1);
			Assert.That(p2.Value, Is.EqualTo(2));
			p1.Value = 2;
			Assert.That(p2.Value, Is.EqualTo(3));
		}
	}
}