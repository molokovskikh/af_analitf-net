using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Castle.Components.DictionaryAdapter;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class SettingsFixture : BaseUnitFixture
	{
		[Test]
		public void Nds_18_markups()
		{
			Env.Current.Settings = new Settings(defaults: true);
			var model = new SettingsViewModel();
			Assert.AreEqual(1, model.Nds18Markups.Value.Count);
		}

		[Test]
		public void Overwrite_markups()
		{
			var addresses = new List<Address> {
				new Address("Тестовый адрес 1"),
				new Address("Тестовый адрес 2"),
			};
			var settings = new Settings(defaults: true);
			Env.Current.Settings = settings;
			Env.Current.Addresses = addresses;
			settings.Markups.Each(x => x.Address = addresses[0]);
			settings.Markups.Add(new MarkupConfig(0, 50, 20, MarkupType.Nds18) {
				Address = addresses[1]
			});
			settings.Markups.Add(new MarkupConfig(50, decimal.MaxValue, 50, MarkupType.Nds18) {
				Address = addresses[1]
			});
			var model = new SettingsViewModel();
			model.Addresses = addresses;
			Assert.AreEqual(1, model.Nds18Markups.Value.Count);
			model.OverwriteNds18Markups = true;
			model.UpdateMarkups();

			var result = settings.Markups.Where(x => x.Type == MarkupType.Nds18 && x.Address == addresses[1]).Implode();
			Assert.AreEqual("Nds18: 0 - 10000 20%", result);
			result = settings.Markups.Where(x => x.Type == MarkupType.Nds18 && x.Address == addresses[0]).Implode();
			Assert.AreEqual("Nds18: 0 - 10000 20%", result);
		}
	}
}