using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Castle.Components.DictionaryAdapter;
using Common.Tools;
using Ionic.Zip;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class SettingsFixture : BaseUnitFixture
	{
		[Test]
		public void Nds_18_markups()
		{
			var address = new Address("Тестовый адрес доставки");
			Env.Current.Settings = new Settings(address);
			Env.Current.Addresses = new [] { address }.ToList();
			var model = new SettingsViewModel();
			model.Address = address;
			Open(model);
			Assert.AreEqual(1, model.Nds18Markups.Value.Count);
		}

		[Test]
		public void Overwrite_markups()
		{
			var addresses = new [] {
				new Address("Тестовый адрес 1"),
				new Address("Тестовый адрес 2"),
			};
			var settings = new Settings(addresses);
			Env.Current.Settings = settings;
			Env.Current.Addresses = addresses.ToList();
			settings.Markups.RemoveEach(x => x.Type == MarkupType.Nds18 && x.Address == addresses[1]);
			settings.Markups.Add(new MarkupConfig(addresses[1], 0, 50, 20, MarkupType.Nds18));
			settings.Markups.Add(new MarkupConfig(addresses[1], 50, decimal.MaxValue, 50, MarkupType.Nds18));
			var model = new SettingsViewModel();
			model.Address = addresses[0];
			Open(model);
			Assert.AreEqual(1, model.Nds18Markups.Value.Count);
			model.OverwriteNds18Markups = true;
			model.Save().ToArray();

			var result = settings.Markups.Where(x => x.Type == MarkupType.Nds18 && x.Address == addresses[1]).Implode();
			Assert.AreEqual("Nds18: 0 - 10000 20%", result);
			result = settings.Markups.Where(x => x.Type == MarkupType.Nds18 && x.Address == addresses[0]).Implode();
			Assert.AreEqual("Nds18: 0 - 10000 20%", result);
		}

		[Test]
		public void Skip_markup_validation_if_no_addresses()
		{
			var settings = new Settings();
			Env.Current.Settings = settings;
			Env.Current.Addresses = new List<Address>();
			var model = new SettingsViewModel();
			var results = model.Save().ToList();
			Assert.AreEqual(0, results.Count, results.Implode());
		}

		[Test]
		public void Go_to_tab_with_error_validation()
		{
			var addresses = new[] {
				new Address("Тестовый адрес 1"),
			};
			var settings = new Settings(addresses);
			Env.Current.Settings = settings;
			Env.Current.Addresses = addresses.ToList();

			var model = new SettingsViewModel();
			model.Address = addresses[0];

			//Очищаем все наценки, в результате должны получить ошибку валидации
			//"Не заданы обязательные интервалы границ цен: [0, 50], [50, 500], [500, 1000000]." для типа MarkupType.VitallyImportant
			settings.Markups.Clear();

			Open(model);

			model.SelectedTab.Value = "AdditionSettingsTab";
			model.Save().ToList();

			Assert.AreEqual("VitallyImportantMarkupsTab", model.SelectedTab.Value);
		}
	}
}