using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;
using System.ComponentModel;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class WaybillTotalFixture
	{
		[Test]
		public void WaybillTotalEventIsRaised()
		{
			var receivedEvents = new List<string>();

			WaybillTotal waybillTotal = new WaybillTotal();

			waybillTotal.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
			{
				receivedEvents.Add(e.PropertyName);
			};

			waybillTotal.TotalSum = 100.5m;
			waybillTotal.TotalRetailSum = 100.5m;

			Assert.AreEqual(2, receivedEvents.Count);
			Assert.AreEqual("TotalSum", receivedEvents[0]);
			Assert.AreEqual("TotalRetailSum", receivedEvents[1]);
		}
	}
}
