using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class SettingsFixture : ViewModelFixture
	{
		private SettingsViewModel model;

		[SetUp]
		public void Setup()
		{
			restore = true;
			model = Init(new SettingsViewModel());
		}

		[Test]
		public void Calculate_base_category()
		{
			model.Settings.GroupByProduct = !model.Settings.GroupByProduct;
			model.Save();
		}

		[Test]
		public void Not_save_invalid_data()
		{
			var origin = model.Markups.Count;
			Assert.AreEqual(0, model.Markups[0].Begin);
			model.Markups.Add(new MarkupConfig());
			model.Save();
			Assert.AreEqual("Некорректно введены границы цен.", manager.MessageBoxes.Implode());
			Close(model);

			model = Init(new SettingsViewModel());
			var all = session.Query<MarkupConfig>().ToList();
			Assert.AreEqual(origin, model.Markups.Count);
			//не должно быть потеряных записей
			Assert.AreEqual(model.Markups.Count + model.VitallyImportantMarkups.Count, all.Count);
		}

		[Test]
		public void Save_changes()
		{
			model.Markups.RemoveEach(model.Markups);
			model.Markups.Add(new MarkupConfig());
			model.Save();
			Close(model);

			Assert.AreEqual("", manager.MessageBoxes.Implode());
			model = Init(new SettingsViewModel());
			Assert.That(model.Markups.Count, Is.EqualTo(1));
		}

		[Test]
		public void Do_not_flush_changes_by_default()
		{
			var value = Generator.Random(10000).First();
			model.Settings.OverCostWarningPercent = value;
			Close(model);
			session.Refresh(settings);
			Assert.That(settings.OverCostWarningPercent, Is.Not.EqualTo(value));
		}

		[Test]
		public void Save_waybill_settings()
		{
			model.CurrentWaybillSettings.Value.Name = "test";
			model.Save();
			Close(model);
			var waybillSettings = session.Load<WaybillSettings>(model.CurrentWaybillSettings.Value.Id);
			Assert.AreEqual("test", waybillSettings.Name);
		}
	}
}