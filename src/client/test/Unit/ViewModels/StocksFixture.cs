using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Client.ViewModels.Inventory;
using System.ComponentModel;
using System.Collections.ObjectModel;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	class StocksFixture : BaseUnitFixture
	{
		[Test]
		public void StockEventIsRaised()
		{
			string actual = null;

			var model = new Stocks();

			model.Items.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
			{
				actual = e.PropertyName;
			};

			model.Items.Value = new List<Stock>();

			Assert.IsNotNull(actual);
			Assert.AreEqual("Value", actual);
			Assert.IsTrue(model.ItemsTotal.Count == 1);
		}
	}
}
