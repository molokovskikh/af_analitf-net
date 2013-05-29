using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class WaybillLineFixture
	{
		private MarkupConfig[] markups;
		private Waybill waybill;
		private Settings settings;

		[SetUp]
		public void Setup()
		{
			markups = new[] {
				new MarkupConfig(0, 10000, 30) {
					MaxMarkup = 35,
					MaxSupplierMarkup = 0,
				},
				new MarkupConfig(0, 50, 20, MarkupType.VitallyImportant) {
					MaxMarkup = 20,
					MaxSupplierMarkup = 0,
				},
				new MarkupConfig(50, 500, 20, MarkupType.VitallyImportant) {
					MaxMarkup = 20,
					MaxSupplierMarkup = 0,
				},
				new MarkupConfig(500, 1000000, 20, MarkupType.VitallyImportant) {
					MaxMarkup = 20,
					MaxSupplierMarkup = 0,
				}
			};
			waybill = new Waybill();
			settings = new Settings();
		}

		[Test]
		public void Calculate_max_markup()
		{
			var line = new WaybillLine(waybill) {
				Nds = 10,
				SupplierCost = 82.63m,
				SupplierCostWithoutNds = 75.12m,
				ProducerCost = 72.89m,
				Quantity = 10
			};
			Calculate(line);
			Assert.AreEqual(35, line.MaxRetailMarkup);
			Assert.AreEqual(107.40, line.RetailCost);
			Assert.AreEqual(1074, line.RetailSum);
			Assert.AreEqual(29.98, line.RetailMarkup);
		}

		[Test]
		public void Round_value()
		{
			var line = new WaybillLine(waybill) {
				Nds = 10,
				SupplierCost = 251.20m,
				SupplierCostWithoutNds = 228.36m,
				ProducerCost = 221.58m,
				Quantity = 1
			};
			Calculate(line, false);
			Assert.AreEqual(326.56, line.RetailCost);
			Assert.AreEqual(30, line.RetailMarkup);
		}

		[Test]
		public void Calculate_without_producer_cost()
		{
			var line = new WaybillLine(waybill) {
				VitallyImportant = true,
				Nds = 10,
				SupplierCost = 196.59m,
				SupplierCostWithoutNds = 178.72m,
				ProducerCost = 164.54m,
				Quantity = 2
			};
			Calculate(line);
			Assert.AreEqual(232.70, line.RetailCost);
		}

		private void Calculate(WaybillLine line, bool round = true)
		{
			line.Calculate(settings, markups, round);
		}
	}
}