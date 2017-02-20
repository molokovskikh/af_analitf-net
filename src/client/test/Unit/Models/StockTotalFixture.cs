using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;
using System.ComponentModel;
using System.Collections.Generic;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	class StockTotalFixture
	{
		[Test]
		public void StockTotalEventIsRaised()
		{
			var receivedEvents = new List<string>();

			StockTotal stockTotal = new StockTotal();

			stockTotal.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
			{
				receivedEvents.Add(e.PropertyName);
			};

			stockTotal.Total = "Итого: ";
			stockTotal.TotalCount = 8.5m;
			stockTotal.TotalSum = 100.5m;
			stockTotal.TotalSumWithNds = 110.5m;
			stockTotal.TotalRetailSum = 100.5m;

			Assert.AreEqual(5, receivedEvents.Count);
			Assert.AreEqual("Total", receivedEvents[0]);
			Assert.AreEqual("TotalCount", receivedEvents[1]);
			Assert.AreEqual("TotalSum", receivedEvents[2]);
			Assert.AreEqual("TotalSumWithNds", receivedEvents[3]);
			Assert.AreEqual("TotalRetailSum", receivedEvents[4]);
		}
	}
}
