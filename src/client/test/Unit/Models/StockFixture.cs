using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
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
				SupplierCost = 50.0m,
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
				Quantity = 8.0m,
				SupplierCost = 50.0m,
				RetailCost = 120.0m
			};

			Assert.AreEqual(400, line.SupplySum);
			Assert.AreEqual(960, line.RetailSum);
		}

		[Test]
		public void Receive_line()
		{
			var waybill = new Waybill(new Address(), new Supplier());
			var line = new WaybillLine(waybill) {
				ProducerCost = 30.57m,
				SupplierCostWithoutNds = 26.80m,
				Nds = 10,
				SupplierCost = 29.48m,
				VitallyImportant = true,
				Quantity = 10
			};
			line.Receive(line.Quantity.Value);
			waybill.AddLine(line);
			var order = new ReceivingOrder(waybill);
			Assert.AreEqual(1, order.ToStocks().Length);
			Assert.AreEqual(1, order.LineCount);
		}
	}
}
