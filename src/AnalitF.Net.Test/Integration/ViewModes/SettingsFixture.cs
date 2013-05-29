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
	public class SettingsFixture : BaseFixture
	{
		private SettingsViewModel model;

		[SetUp]
		public void Setup()
		{
			model = Init(new SettingsViewModel());
		}

		[Test]
		public void Calculate_base_category()
		{
			model.Settings.GroupByProduct = !model.Settings.GroupByProduct;
			model.Save();
		}

		[Test]
		public void Save_changes()
		{
			var count = model.Markups.Count;
			model.Markups.Add(new MarkupConfig());
			model.Save();
			model = Init(new SettingsViewModel());
			Assert.That(model.Markups.Count, Is.EqualTo(count + 1));
		}

		[Test]
		public void Do_not_flush_changes_by_default()
		{
			var value = Generator.Random(10000).First();
			model.Settings.OverCostWarningPercent = value;
			ScreenExtensions.TryDeactivate(model, true);
			session.Refresh(settings);
			Assert.That(settings.OverCostWarningPercent, Is.Not.EqualTo(value));
		}

		[Test]
		public void Save_waybill_settings()
		{
			model.CurrentWaybillSettings.Value.Name = "test";
			model.Save();
			ScreenExtensions.TryDeactivate(model, true);
			var waybillSettings = session.Load<WaybillSettings>(model.CurrentWaybillSettings.Value.Id);
			Assert.AreEqual("test", waybillSettings.Name);
		}
	}
}