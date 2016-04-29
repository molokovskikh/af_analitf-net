using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class SpecialMarkupFixture : ViewModelFixture
	{
		public List<Address> AddressList { get; set; }

		[SetUp]
		public void Setup()
		{
			restore = true;

			AddressList = session.Query<Address>().ToList();
			if (AddressList.Count <= 1) {
				session.Save(new Address("Тестовый адрес 1"));
				session.Save(new Address("Тестовый адрес 2"));
				AddressList = session.Query<Address>().ToList();
			}
			address = AddressList[1];
		}

		[Test]
		public void SpecialMarkup_MarkUpsAdding()
		{
			// подготовка данных

			settings.Markups.RemoveEach(settings.Markups.Where(s => s.Type == MarkupType.Special).ToList());
			session.Flush();

			// задаем свойства маркера "Специальной наценки"
			Env.Current.Settings = settings;
			Env.Current.Addresses = AddressList.ToList();
			settings.Markups.RemoveEach(x => x.Type == MarkupType.Special && x.Address == AddressList[1]);
			var newSpecialMarkUpA = new MarkupConfig(AddressList[0], 0, 50, 20, MarkupType.Special);
			var newSpecialMarkUpB = new MarkupConfig(AddressList[1], 50, 10000, 50, MarkupType.Special);
			newSpecialMarkUpA.Settings = settings;
			newSpecialMarkUpB.Settings = settings;
			settings.Markups.Add(newSpecialMarkUpA);
			settings.Markups.Add(newSpecialMarkUpB);
			session.Save(settings);
			session.Flush();
			// проверяем, выводятся ли заданные свойства маркера
			var result = settings.Markups.Where(x => x.Type == MarkupType.Special && x.Address == AddressList[1]).Implode();
			Assert.AreEqual("Special: 50 - 10000 50%", result);
			result = settings.Markups.Where(x => x.Type == MarkupType.Special && x.Address == AddressList[0]).Implode();
			Assert.AreEqual("Special: 0 - 50 20%", result);

			// проверяем, выводятся ли заданные свойства маркера
			var model = new SettingsViewModel();
			model.Shell = shell;
			model.Address = AddressList[1];
			Open(model);
			Assert.Greater(model.Products.Value.Count, 0);
			Assert.AreEqual(1, model.SpecialMarkups.Value.Count);
			Assert.AreEqual(true, model.SpecialMarkups.Value.Any(s => s.Type == MarkupType.Special && s.End == 10000));
			model.Save().ToList();
			Close(model);
			// проверяем, не изменились ли свойства маркера после открытия/закрытия формы
			session.Refresh(settings);
			result = settings.Markups.Where(x => x.Type == MarkupType.Special && x.Address == AddressList[1]).Implode();
			Assert.AreEqual("Special: 50 - 10000 50%", result);
			result = settings.Markups.Where(x => x.Type == MarkupType.Special && x.Address == AddressList[0]).Implode();
			Assert.AreEqual("Special: 0 - 50 20%", result);
		}

		[Test]
		public void Settings()
		{
			var model = new SettingsViewModel();
			Open(model);
			//выделяем препарат, добавляем его в список маркерованных "Специальной наценкой"
			model.CurrentProduct.Value = model.Products.Value.First();
			model.SpecialMarkupCheck();
			Assert.AreEqual(1, model.SpecialMarkupProducts.Value.Count);
			//выделяем препарат, удаляем его из списка маркерованных "Специальной наценкой"
			model.CurrentSpecialMarkupProduct.Value = model.SpecialMarkupProducts.Value.First();
			model.SpecialMarkupUncheck();
			Assert.AreEqual(0, model.SpecialMarkupProducts.Value.Count);
		}

		[Test]
		public void SpecialMarkup_Waybill()
		{
			// подготовка данных
			var specialMurkupCatalog = session.Query<SpecialMarkupCatalog>().ToList();
			session.DeleteEach(specialMurkupCatalog);
			var productsList = session.Query<Product>().Take(10).ToList();
			session.Flush();
			var address = session.Query<Address>().First();
			var settings = session.Query<Settings>().First();
			settings.Markups.RemoveEach(settings.Markups.Where(s => s.Type == MarkupType.Special).ToList());

			// создаем накладную
			var waybill = new Waybill(address, session.Query<Supplier>().First());
			waybill.Address = AddressList[1];
			waybill.Lines = new List<WaybillLine>();

			// открываем форму настроек
			var model = new SettingsViewModel();
			model.Shell = shell;
			model.Address = AddressList[1];
			Open(model);

			for (var i = 0; i < 10; i++) {
				// заполняем список маркерованных "Специальной наценкой" препаратов
				model.CurrentProduct.Value = model.Products.Value.Skip(i).FirstOrDefault();
				model.SpecialMarkupCheck();
				//создаем связи отмеченного каталога со спецификациями
				var catalog = session.Query<Catalog>().FirstOrDefault(s => s.Id == model.CurrentProduct.Value.CatalogId);

				var product = productsList[i];
				//	product.Catalog = catalog;
				product.CatalogId = catalog.Id;
				session.Update(product);
				session.Flush();

				// добавляем выбранную спецификацию препарата в накладной
				var wb = new WaybillLine(waybill) {
					Quantity = 10,
					Nds = 10,
					ProducerCost = 15.13m,
					SupplierCostWithoutNds = 18.25m,
					SupplierCost = 20.8m,
					Product = catalog.FullName + " " + catalog.Form,
					Certificates = "РОСС RU.ФМ08.Д38737",
					Period = "01.05.2017",
					Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
					RegistryCost = 382.89m,
					SupplierPriceMarkup = -5.746m,
					VitallyImportant = true,
					SerialNumber = "21012",
					Amount = 442.05m,
					NdsAmount = 40.19m,
					BillOfEntryNumber = "10609010/101209/0004305/1",
					//для отчета по жизененно важным
					EAN13 = "4606915000379"
				};
				wb.ProductId = product.Id;
				waybill.Lines.Add(wb);
				session.Save(waybill);
				session.Flush();
			}
			model.Save().ToList();
			session.Refresh(waybill);
			waybill.Calculate(settings, shell.SpecialMarkupProducts.Value);
			session.Update(waybill);
			session.Flush();
			session.Refresh(waybill);

			// всего должно быть маркеровано 10 препаратов
			Assert.AreEqual(10, model.SpecialMarkupProducts.Value.Count);

			// при пересчете маркер "Специальной наценки" не должен влиять на стоимость, т.к. не указаны его свойства
			var waybillModel = Open(new WaybillDetails(waybill.Id));
			var waybillLine = waybillModel.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(null, waybillLine.RetailCost);
			Close(waybillModel);

			// задаем свойства маркера "Специальной наценки"
			Env.Current.Settings = settings;
			Env.Current.Addresses = AddressList.ToList();
			settings.Markups.RemoveEach(x => x.Type == MarkupType.Special && x.Address == AddressList[1]);
			var newSpecialMarkUpB = new MarkupConfig(AddressList[1], 0, 10000, 50, MarkupType.Special);
			newSpecialMarkUpB.Settings = settings;
			settings.Markups.Add(newSpecialMarkUpB);
			foreach (var newMarkupConfig in MarkupConfig.Defaults(address)) {
				settings.Markups.Add(newMarkupConfig);
				newMarkupConfig.Settings = settings;
			}
			session.Save(settings);
			session.Flush();
			// проверяем, выводятся ли заданные свойства маркера
			var result = settings.Markups.Where(x => x.Type == MarkupType.Special && x.Address == AddressList[1]).Implode();
			Assert.AreEqual("Special: 0 - 10000 50%", result);

			// проверяем, вероно ли отражаются свойства маркера на цене препаратов
			session.Refresh(waybill);

			waybill.Calculate(settings, shell.SpecialMarkupProducts.Value);
			session.Save(waybill);
			session.Flush();

			//обновляем форму накладной, проверяем наценку
			waybillModel = Open(new WaybillDetails(waybill.Id));
			waybillLine = waybillModel.Lines.Value.Cast<WaybillLine>().First();
			Assert.AreEqual(30.8M, waybillLine.RetailCost);
		}

		[Test]
		public void SpecialMarkup_Offer()
		{
			// подготовка данных
			var specialMurkupCatalog = session.Query<SpecialMarkupCatalog>().ToList();
			session.DeleteEach(specialMurkupCatalog);
			session.Flush();
			var settings = session.Query<Settings>().First();
			settings.Markups.RemoveEach(settings.Markups.Where(s => s.Type == MarkupType.Special).ToList());
			var catalog = session.Query<Catalog>()
				.First(c => c.HaveOffers
					&& session.Query<Offer>().Count(o => o.CatalogId == c.Id && !o.VitallyImportant) >= 2
					&& !c.VitallyImportant);

			// добавляем Спецальные наценки
			settings.Markups.Clear();
			var markupType = MarkupType.Special;
			foreach (var newMarkupConfig in MarkupConfig.Defaults(address)) {
				settings.Markups.Add(newMarkupConfig);
				newMarkupConfig.Settings = settings;
			}
			settings.AddMarkup(new MarkupConfig(address, 0, 700, 20, markupType));
			settings.AddMarkup(new MarkupConfig(address, 700, 7000, 30, markupType));
			session.Save(settings);
			session.Flush();

			//проверяем наличие предложений
			var model = new CatalogOfferViewModel(catalog);
			Open(model);
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));

			// открываем форму настроек
			var settingsView = new SettingsViewModel();
			settingsView.Shell = shell;
			settingsView.Address = address;
			Open(settingsView);
			//добавляем помечаем интересующие каталоги маркером "Спецальной наценки"
			for (var i = 0; i < model.Offers.Value.Count; i++) {
				settingsView.SpecialMarkupSearchText.Value = model.Offers.Value[i].ProductSynonym.Substring(0, 4);
				settingsView.Products.Value = settingsView.SearchProduct(session.SessionFactory.OpenStatelessSession());
				settingsView.CurrentProduct.Value =
					settingsView.Products.Value.First(s => s.CatalogId == model.Offers.Value[i].CatalogId);
				settingsView.SpecialMarkupCheck();
			}
			settingsView.Save().ToList();
			session.Flush();
			Close(model);
			//обновляем форму предложения, проверяем наценки
			model = new CatalogOfferViewModel(catalog);
			Open(model);
			//для Специальной наценки диапазона 0 - 700
			model.CurrentOffer.Value = null;
			model.CurrentOffer.Value = model.Offers.Value[0];
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(20), model.CurrentOffer.Value.Id.ToString());
			var expected = Math.Round(model.Offers.Value[0].Cost*(decimal) 1.2, 2);
			scheduler.AdvanceByMs(1000);
			Assert.That(model.RetailCost.Value, Is.EqualTo(expected));

			//для Специальной наценки диапазона 700 - 7000
			model.CurrentOffer.Value = model.Offers.Value[1];
			scheduler.AdvanceByMs(1000);
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(30));
			expected = Math.Round(model.Offers.Value[1].Cost*(decimal) 1.3, 2);
			Assert.That(model.RetailCost.Value, Is.EqualTo(expected));

			//проверяем соххранение значения наценки при ручном вводе
			model.RetailMarkup.Value = 23;
			model.CurrentOffer.Value = model.Offers.Value[0];
			scheduler.AdvanceByMs(1000);
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(23));
		}
	}
}