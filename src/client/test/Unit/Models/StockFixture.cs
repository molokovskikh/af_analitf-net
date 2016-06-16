using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	class StockFixture
	{
		[Test]
		public void Calculate_markup()
		{
			var line = new Stock()
			{
				Cost = 50.0m,
				RetailCost = 120.0m,
				LowCost = 100.0m,
				OptCost = 110.0m
			};

			Assert.AreEqual(140, line.RetailMarkup);
			Assert.AreEqual(100, line.LowMarkup);
			Assert.AreEqual(120, line.OptMarkup);
		}

		[Test]
		public void Calculate_sum()
		{
			var line = new Stock()
			{
				Count = 8.0m,
				Cost = 50.0m,
				RetailCost = 120.0m
			};

			Assert.AreEqual(400, line.Sum);
			Assert.AreEqual(960, line.RetailSum);
		}
	}
}
