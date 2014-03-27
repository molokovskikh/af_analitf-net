using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class CollectionHelperFixture
	{
		[Test]
		public void Create_live_projection()
		{
			var items = new List<int> { 1, 2, 3, 4 };
			var even = items
				.Where(i => i % 2 == 0)
				.LinkTo(items);
			even.Remove(2);
			Assert.AreEqual("1, 3, 4", items.Implode());
		}
	}
}