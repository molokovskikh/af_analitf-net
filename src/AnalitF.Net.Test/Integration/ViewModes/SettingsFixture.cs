using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
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
	}
}