using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class WaybillFixture
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
				},
				new MarkupConfig(0, 50, 20, MarkupType.VitallyImportant) {
					MaxMarkup = 20,
				},
				new MarkupConfig(50, 500, 20, MarkupType.VitallyImportant) {
					MaxMarkup = 20,
				},
				new MarkupConfig(500, 1000000, 20, MarkupType.VitallyImportant) {
					MaxMarkup = 20,
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
			waybill.RoundTo1 = false;
			var line = new WaybillLine(waybill) {
				Nds = 10,
				SupplierCost = 251.20m,
				SupplierCostWithoutNds = 228.36m,
				Quantity = 1
			};
			Calculate(line);
			Assert.AreEqual(326.55, line.RetailCost);
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

		[Test]
		public void Calculate_without_producer_cost1()
		{
			var line = new WaybillLine(waybill) {
				Nds = 10,
				SupplierCost = 370.35m,
				SupplierCostWithoutNds = 336.68m,
				ProducerCost = 327.27m,
				Quantity = 2
			};
			Calculate(line);
			Assert.AreEqual(481.40, line.RetailCost);
		}

		[Test]
		public void Calculate_real_retail_markup()
		{
			var line = new WaybillLine(waybill) {
				SupplierCost = 34.10m,
				SupplierCostWithoutNds = 31m,
				Nds = 10,
				ProducerCost = 32.39m,
				VitallyImportant = true,
			};
			Calculate(line);
			Assert.AreEqual(41.20, line.RetailCost);
			Assert.AreEqual(19.93m, line.RetailMarkup);
			Assert.AreEqual(20.82m, line.RealRetailMarkup);
		}

		[Test]
		public void Recalculate_retail_cost()
		{
			var line = Line();
			Calculate(line);
			Assert.AreEqual(55.70, line.RetailCost);
			Assert.AreEqual(29.84, line.RealRetailMarkup);
			var items = Subscribe(line.Changed());
			line.RetailMarkup = 50;
			Assert.AreEqual(64.30, line.RetailCost);
			Assert.AreEqual(49.88, line.RetailMarkup);
			Assert.AreEqual(49.88, line.RealRetailMarkup);
			Assert.That(items.Select(e => e.PropertyName),
				Is.EquivalentTo(new[] {
					"RetailCost",
					"RetailSum",
					"RealRetailMarkup",
					"RetailMarkup",
					"IsMarkupInvalid",
					"IsMarkupToBig"
				}));
		}

		[Test]
		public void Recalculate_on_retail_cost_change()
		{
			var line = Line();
			Calculate(line);

			Assert.AreEqual(55.70, line.RetailCost);
			Assert.AreEqual(29.84, line.RealRetailMarkup);
			var items = Subscribe(line);
			line.RetailCost = 50;
			Assert.AreEqual(50, line.RetailCost);
			Assert.AreEqual(16.55, line.RetailMarkup);
			Assert.That(items.Select(e => e.PropertyName),
				Is.EquivalentTo(new[] {
					"RealRetailMarkup",
					"RetailMarkup",
					"IsMarkupInvalid",
					"IsMarkupToBig",
					"RetailSum",
					"RetailCost"
				}));
			Assert.AreEqual(500, line.Waybill.RetailSum);
		}

		[Test]
		public void Do_not_recalculate_edited_cost()
		{
			var line = Line();
			waybill.Calculate(settings, markups);
			Assert.AreEqual(557, waybill.RetailSum);
			line.RetailCost = 60;
			waybill.Calculate(settings, markups);
			Assert.AreEqual(600, waybill.RetailSum);
		}

		[Test]
		public void Check_max_supplier_markup()
		{
			markups[0].MaxSupplierMarkup = 5;
			var line = Line();
			Calculate(line);
			line.SupplierPriceMarkup = 10;
			Assert.IsTrue(line.IsSupplierPriceMarkupInvalid);
		}

		[Test]
		public void Recalculate_validation_status()
		{
			var line = Line();
			Calculate(line);
			var changes = Subscribe(line);
			line.RetailCost = 100;
			Assert.IsTrue(line.IsMarkupToBig);
			Assert.That(changes.Implode(e => e.PropertyName), Is.StringContaining("IsMarkupToBig"));
		}

		[Test]
		public void Recalculate_on_real_markup_change()
		{
			var line = new WaybillLine(waybill) {
				SupplierCost = 34.10m,
				SupplierCostWithoutNds = 31m,
				Nds = 10,
				ProducerCost = 32.39m,
				Quantity = 10,
				VitallyImportant = true
			};
			Calculate(line);
			line.RealRetailMarkup = 30;
			Assert.AreEqual(44.30, line.RetailCost);
			Assert.AreEqual(29.91, line.RealRetailMarkup);
			Assert.AreEqual(28.63, line.RetailMarkup);
		}

		private WaybillLine Line()
		{
			markups[0].Markup = 30;
			var line = new WaybillLine(waybill) {
				SupplierCost = 42.90m,
				SupplierCostWithoutNds = 36.36m,
				Nds = 18,
				ProducerCost = 28.78m,
				Quantity = 10
			};
			waybill.Lines.Add(line);
			return line;
		}
		private static List<PropertyChangedEventArgs> Subscribe(INotifyPropertyChanged line)
		{
			return Subscribe(line.Changed());
		}

		private static List<PropertyChangedEventArgs> Subscribe(
			IObservable<EventPattern<PropertyChangedEventArgs>> observable)
		{
			var items = new List<PropertyChangedEventArgs>();
			observable.Subscribe(e => items.Add(e.EventArgs));
			return items;
		}

		private void Calculate(WaybillLine line)
		{
			line.Calculate(settings, markups);
		}
	}
}