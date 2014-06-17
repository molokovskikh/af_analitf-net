using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class MinCostFixture
	{
		[Test]
		public void Cal_diff()
		{
			var cost = new MinCost {
				Cost = 100,
				NextCost = 150,
			};
			Assert.AreEqual(50, cost.Diff);
		}
	}
}