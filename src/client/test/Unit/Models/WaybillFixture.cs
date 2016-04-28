using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class WaybillFixture
	{
		private Waybill waybill;
		private Settings settings;
		private WaybillSettings waybillSettings;
		private Address address;

		[SetUp]
		public void Setup()
		{
			address = new Address("Тестовый");
			settings = new Settings(address);
			settings.Markups.Each(x => x.Address = address);
			waybillSettings = new WaybillSettings();
			settings.Waybills.Add(waybillSettings);

			waybill = new Waybill {
				Address = address
			};
		}

		[Test(Description = "Проверка работы флага из настроек накладных ЖНВЛС - 'Использовать цену завода с НДС при определении ценового диапозона'")]
		public void Calculate_markup_with_supplierPriceWithNDS_flag()
		{
			//Подстраиваем диапазоны наценок под товар в накладной
			var markups = settings.Markups;
			var minorMarkup = markups.First(x => x.Begin == 0 && x.Type == MarkupType.VitallyImportant);
			minorMarkup.End = 78;
			var majorMarkup = markups.First(x => x.Begin == 50 && x.Type == MarkupType.VitallyImportant);
			majorMarkup.Begin = minorMarkup.End;
			majorMarkup.Markup = 40;
			majorMarkup.MaxMarkup = 40;

			var line = new WaybillLine(waybill)
			{
				Nds = 10,
				SupplierCost = 82.63m,
				SupplierCostWithoutNds = 75.12m,
				ProducerCost = 72.89m,
				Quantity = 10,
				VitallyImportant = true
			};
			//Стандартный расклад
			Calculate(line);
			Assert.AreEqual(19.92, line.RetailMarkup);

			//Проверяем расчет с флагом
			settings.UseSupplierPriceWithNdsForMarkup = true;
			Calculate(line);

			Assert.That(line.ProducerCostWithTax, Is.GreaterThan(majorMarkup.Begin));
			Assert.AreEqual(40, line.RetailMarkup);
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
			Assert.AreEqual(20, line.MaxRetailMarkup);
			Assert.AreEqual(99.1, line.RetailCost);
			Assert.AreEqual(991, line.RetailSum);
			Assert.AreEqual(19.93, line.RetailMarkup);
		}

		[Test]
		public void Round_value()
		{
			settings.Rounding = Rounding.None;
			var line = new WaybillLine(waybill) {
				Nds = 10,
				SupplierCost = 251.20m,
				SupplierCostWithoutNds = 228.36m,
				Quantity = 1
			};
			Calculate(line);
			Assert.AreEqual(301.44, line.RetailCost);
			Assert.AreEqual(20, line.RetailMarkup);
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
			Assert.AreEqual(444.4, line.RetailCost);
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
			var items = RxHelper.CollectChanges(line);
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
			var line = Calculate(Line());

			Assert.AreEqual(55.70, line.RetailCost);
			Assert.AreEqual(29.84, line.RealRetailMarkup);
			var items = line.CollectChanges();
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
			var line = Calculate(Line());
			Assert.AreEqual(557, waybill.RetailSum);
			line.RetailCost = 60;
			waybill.Calculate(settings, new List<uint>());
			Assert.AreEqual(600, waybill.RetailSum);
		}

		[Test]
		public void Check_max_supplier_markup()
		{
			settings.Markups[0].MaxSupplierMarkup = 5;
			var line = Calculate(Line());
			line.SupplierPriceMarkup = 10;
			Assert.IsTrue(line.IsSupplierPriceMarkupInvalid);
		}

		[Test]
		public void Recalculate_validation_status()
		{
			var line = Line();
			Calculate(line);
			var changes = line.CollectChanges();
			line.RetailCost = 100;
			Assert.IsTrue(line.IsMarkupToBig);
			Assert.That(changes.Implode(e => e.PropertyName), Does.Contain("IsMarkupToBig"));
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

		[Test]
		public void Waybill_as_vitally_important()
		{
			var line = Line();
			line.Nds = 10;
			line.SupplierCostWithoutNds = Math.Round(line.SupplierCost.Value / 1.1m, 2);
			Calculate(line);
			Assert.IsTrue(waybill.CanBeVitallyImportant);
			waybill.VitallyImportant = true;
			Assert.AreEqual(true, waybill.VitallyImportant);
			Assert.AreEqual(49.2, line.RetailCost);
			Assert.AreEqual(492, waybill.RetailSum);
		}

		[Test]
		public void Ignore_nds()
		{
			waybillSettings.Taxation = Taxation.Envd;
			waybillSettings.IncludeNds = false;
			var line = Line();
			Calculate(line);
			Assert.AreEqual(53.8, line.RetailCost);
		}

		[Test]
		public void Do_not_change_markup_on_tax_factor_change()
		{
			var line = Line();
			line.VitallyImportant = true;
			Calculate(line);

			waybillSettings.IncludeNdsForVitallyImportant = false;
			line.RetailMarkup = 10;
			Assert.AreEqual(9.73, line.RetailMarkup);

			line.Edited = false;
			waybillSettings.IncludeNdsForVitallyImportant = true;
			line.RetailMarkup = 10;
			Assert.AreEqual(9.72, line.RetailMarkup);
		}

		[Test]
		public void Calculate_fields_for_user_created_waybill()
		{
			waybill.Calculate(settings, new List<uint>());
			waybill.IsCreatedByUser = true;
			var line = new WaybillLine();
			waybill.AddLine(line);

			line.SupplierCostWithoutNds = 67.5m;
			line.SupplierCost = 75;
			line.Nds = 10;
			line.Quantity = 2;

			line.EndEdit();

			Assert.AreEqual(150, line.Amount);
			Assert.AreEqual(150, waybill.Sum);
			Assert.AreEqual(89.8, line.RetailCost);
		}

		[Test]
		public void Notify_on_uncalculable_lines()
		{
			var waybillLine = new WaybillLine(waybill) {
				Nds = 10,
				Quantity = 20,
				SupplierCost = 9.72m,
				SupplierCostWithoutNds = 8.84m
			};
			Calculate(waybillLine);
			Assert.IsTrue(waybill.CanBeVitallyImportant);
			var changes = waybillLine.CollectChanges();
			waybill.VitallyImportant = true;
			var props = changes.Select(c => c.PropertyName).ToArray();
			Assert.IsNull(waybillLine.RetailCost);
			Assert.Contains("IsNdsInvalid", props);
			Assert.Contains("IsMarkupToBig", props);
			Assert.Contains("ActualVitallyImportant", props);
			Assert.Contains("RetailCost", props);
		}

		[Test]
		public void Invalid_nds()
		{
			var waybillLine = new WaybillLine(waybill) {
				Nds = 18,
				Quantity = 20,
				SupplierCost = 9.72m,
				SupplierCostWithoutNds = 8.84m
			};
			Calculate(waybillLine);
			Assert.IsTrue(waybill.CanBeVitallyImportant);
			waybill.VitallyImportant = true;
			Assert.IsTrue(waybillLine.IsNdsInvalid);
		}

		[Test]
		public void Do_not_recalc_markup()
		{
			settings.Rounding = Rounding.None;
			var line = new WaybillLine(waybill) {
				ProducerCost = 30.57m,
				SupplierCostWithoutNds = 26.80m,
				Nds = 10,
				SupplierCost = 29.48m,
				VitallyImportant = true,
			};
			waybill.AddLine(line);
			waybill.Calculate(settings, new List<uint>());
			Assert.AreEqual(20, line.RetailMarkup);
			Assert.AreEqual(22.81, line.RealRetailMarkup);
			Assert.AreEqual(36.21, line.RetailCost);
		}

		[Test]
		public void Calculate_markup_not_include_nds()
		{
			var markup = settings.Markups.First(m => m.Type == MarkupType.Nds18);
			markup.Markup = 60;
			markup.MaxMarkup = 60;
			settings.Rounding = Rounding.None;
			waybillSettings.IncludeNds = false;
			var line = new WaybillLine(waybill) {
				SupplierCostWithoutNds = 185.50m,
				Nds = 18,
				SupplierCost = 218.89m,
			};
			Calculate(line);
			Assert.AreEqual(60, line.MaxRetailMarkup);
			Assert.AreEqual(60, line.RetailMarkup);
			Assert.AreEqual(50.85, line.RealRetailMarkup);
			Assert.AreEqual(330.19, line.RetailCost);
		}

		[Test]
		public void Roung_to_1_00()
		{
			settings.Rounding = Rounding.To1_00;
			var line = new WaybillLine(waybill) {
				Nds = 10,
				SupplierCost = 251.20m,
				SupplierCostWithoutNds = 228.36m,
				Quantity = 1
			};
			Calculate(line);
			Assert.AreEqual(301, line.RetailCost);
			Assert.AreEqual(19.83, line.RetailMarkup);
		}

		[Test]
		public void Do_not_recalculate_edited_lines()
		{
			var line = new WaybillLine(waybill) {
				Product = "ТЕСТ-ПОЛОСКИ",
				SupplierCostWithoutNds = 1298.18m,
				Nds = 10,
				SupplierCost = 1428,
			};
			Calculate(line);
			Assert.AreEqual(1713.6, line.RetailCost);

			line.RetailCost = 5000;
			Assert.AreEqual(5000, line.RetailCost);
			Assert.AreEqual(250.14, line.RetailMarkup);

			waybill.Calculate(settings, new List<uint>());
			Assert.AreEqual(5000, line.RetailCost);
		}

		[Test]
		public void Calculate_markup_on_producer_cost()
		{
			settings.Markups.First(x => x.Type == MarkupType.VitallyImportant && x.Begin == 0)
				.Markup = 23;
			settings.Markups.First(x => x.Type == MarkupType.VitallyImportant && x.Begin == 50)
				.Markup = 18;
			settings.Markups.First(x => x.Type == MarkupType.VitallyImportant && x.Begin == 500)
				.Markup = 12;
			var line = new WaybillLine(waybill) {
				Product = "АМИКСИН 0,125 N6 ТАБЛ П/ПЛЕН/ОБОЛОЧ",
				VitallyImportant = true,
				ProducerCost = 501.91m,
				Nds = 10,
				SupplierCost = 529.65m,
				SupplierCostWithoutNds = 481.50m,
				Quantity = 3
			};
			Calculate(line);
			Assert.AreEqual(595.90, line.RetailCost);
			Assert.AreEqual(12.00, line.RetailMarkup);
			Assert.AreEqual(12.51, line.RealRetailMarkup);
		}

		[Test]
		public void Calculate_with_nds_18()
		{
			var markup = settings.Markups.First(x => x.Type == MarkupType.Nds18);
			markup.Markup = 38;
			var line = new WaybillLine(waybill) {
				Nds = 18,
				SupplierCost = 251.20m,
				SupplierCostWithoutNds = 228.36m,
				Quantity = 1
			};
			Calculate(line);
			Assert.AreEqual(353.60, line.RetailCost);
			Assert.AreEqual(37.96, line.RetailMarkup);

			line.VitallyImportant = true;
			Calculate(line);
			Assert.AreEqual(353.60, line.RetailCost);
			Assert.AreEqual(37.96, line.RetailMarkup);
		}

		[Test]
		public void Calculate_with_zero_producer_cost()
		{
			var markup = settings.Markups.First(x => x.Type == MarkupType.Nds18);
			markup.Markup = 38;
			var line = new WaybillLine(waybill) {
				Nds = 18,
				SupplierCost = 251.20m,
				SupplierCostWithoutNds = 228.36m,
				ProducerCost = 0,
				Quantity = 1,
				VitallyImportant = true
			};
			Calculate(line);
			Assert.AreEqual(353.60, line.RetailCost);
			Assert.AreEqual(37.96, line.RetailMarkup);
		}

		private WaybillLine Line()
		{
			settings.Markups.First(x => x.Type == MarkupType.Nds18).Markup = 30;
			var line = new WaybillLine(waybill) {
				SupplierCost = 42.90m,
				SupplierCostWithoutNds = 36.36m,
				Nds = 18,
				ProducerCost = 28.78m,
				Quantity = 10
			};
			return line;
		}

		private WaybillLine Calculate(WaybillLine line)
		{
			waybill.WaybillSettings = waybillSettings;
			waybill.Lines.Add(line);
			waybill.Calculate(settings, new List<uint>());
			return line;
		}
	}
}