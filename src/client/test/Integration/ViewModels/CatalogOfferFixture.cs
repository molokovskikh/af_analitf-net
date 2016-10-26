using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.NHibernate;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class CatalogOfferFixture : ViewModelFixture
	{
		private Lazy<CatalogOfferViewModel> lazyModel;
		private Catalog catalog;

		private CatalogOfferViewModel model => lazyModel.Value;

		[SetUp]
		public void Setup()
		{
			lazyModel = new Lazy<CatalogOfferViewModel>(() => Open(new CatalogOfferViewModel(catalog)));
			catalog = session.Query<Catalog>()
				.First(c => c.HaveOffers
					&& session.Query<Offer>().Count(o => o.CatalogId == c.Id && !o.VitallyImportant) >= 2
					&& !c.VitallyImportant);
		}

		[Test]
		public void Filter_by_producer()
		{
			restore = true;

			var baseOffer = session.Query<Offer>().First(o => o.CatalogId == catalog.Id);
			var newOffer = new Offer(baseOffer.Price, baseOffer, baseOffer.Cost + 50) {
				Producer = "Тестовый",
				ProducerId = (uint?)Generator.Random().First()
			};
			newOffer.Id.OfferId += (ulong)Generator.Random().First();
			session.Save(newOffer);

			var count = model.Offers.Value.Count;
			Assert.That(count, Is.GreaterThan(2));
			model.CurrentProducer.Value = model.Producers.Value[1];
			Assert.That(model.Offers.Value.Count, Is.LessThan(count));
		}

		[Test, Ignore("Нестабильный")]
		public void Filter_by_producer_SavingState()
		{
			//проверяем отсутствие флагов до сохранения фильтра
			Assert.That(model.CanSaveFilterProducer.Value, Is.EqualTo(false));
			Assert.That(model.CurrentProducer.Value.Id, Is.EqualTo(0));
			//выставляем флаг "сохранения фильтра"
			model.CanSaveFilterProducer.Value = true;
			model.Filter();
			//проверяем количемтво выводимых записей при не заполненном фильтре
			var maxCount = model.Offers.Value.Count;
			Assert.That(model.Offers.Value.Count, Is.EqualTo(maxCount));

			//устанавливаем фильтрацию по одному поставщику
			model.CurrentProducer.Value = model.Producers.Value[1];
			scheduler.AdvanceByMs(1000);
			Assert.That(model.Offers.Value.Count, Is.LessThan(maxCount));
			//закрываем форму
			model.TryClose();
			Close(model);
			var modelNew = Open(new CatalogOfferViewModel(catalog));
			modelNew.Filter();
			//проверяем наличие флагов после сохранения фильтра
			Assert.That(modelNew.CanSaveFilterProducer.Value, Is.EqualTo(true));
			Assert.That(modelNew.CurrentProducer.Value.Id, Is.Not.EqualTo(0));
			Assert.That(modelNew.Offers.Value.Count, Is.LessThan(maxCount));

			//убираем флаг "сохранения фильтра"
			modelNew.CanSaveFilterProducer.Value = false;
			modelNew.CurrentProducer.Value = modelNew.Producers.Value.FirstOrDefault(s => s.Id == 0);
			modelNew.Filter();
			//проверяем количемтво выводимых записей при заполненном фильтре на новой форме
			Assert.That(modelNew.Offers.Value.Count, Is.EqualTo(maxCount));
			//закрываем форму
			modelNew.TryClose();
			//проверяем наличие флагов после сохранения фильтра
			Assert.That(modelNew.CanSaveFilterProducer.Value, Is.EqualTo(false));
			Assert.That(modelNew.CurrentProducer.Value.Id, Is.EqualTo(0));
		}

		[Test]
		public void Calculate_retail_cost()
		{
			var offer = session.Query<Offer>()
				.Where(o => o.CatalogId == catalog.Id)
				.OrderBy(o => o.Cost)
				.First();
			var splitCost = offer.Cost;

			settings.Markups.Clear();
			//позиция не может быть жизненно важной тк мы не генерируем таких тестовых данных
			var markupType = MarkupType.Over;
			settings.AddMarkup(new MarkupConfig(address, 0, splitCost, 20, markupType));
			settings.AddMarkup(new MarkupConfig(address, splitCost, decimal.MaxValue, 30, markupType));
			session.Save(settings);

			Assert.That(model.Offers.Value[0].RetailCost, Is.Not.EqualTo(0));

			model.CurrentOffer.Value = null;
			model.CurrentOffer.Value = model.Offers.Value[0];
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(20), model.CurrentOffer.Value.Id.ToString());
			var expected = Math.Round(model.Offers.Value[0].Cost * (decimal)1.2, 2);
			Assert.That(model.RetailCost.Value, Is.EqualTo(expected));

			model.CurrentOffer.Value = model.Offers.Value[1];
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(30), "цена разделитель {0} текущая {1}", splitCost, model.CurrentOffer.Value.Cost);
			expected = Math.Round(model.Offers.Value[1].Cost * (decimal)1.3, 2);
			Assert.That(model.RetailCost.Value, Is.EqualTo(expected));

			model.RetailMarkup.Value = 23;
			model.CurrentOffer.Value = model.Offers.Value[0];
			Assert.That(model.RetailMarkup.Value, Is.EqualTo(23));
		}

		[Test]
		public void Calculate_diff()
		{
			Assert.That(model.Offers.Value[0].Diff, Is.Null);
			Assert.That(model.Offers.Value[1].Diff, Is.Not.EqualTo(0));
		}

		[Test]
		public void Select_base_offer()
		{
			catalog = FindMultiOfferCatalog();
			MakeDifferentCategory(catalog);

			var baseOffer = model.Offers.Value.First(o => o.Price.BasePrice);
			Assert.That(model.CurrentOffer.Value.Id, Is.EqualTo(baseOffer.Id), model.Offers.Value.Implode(o => o.Id));
		}

		[Test]
		public void Filter_by_price_category()
		{
			catalog = FindMultiOfferCatalog();
			MakeDifferentCategory(catalog);

			var baseCount = model.Offers.Value.Count(o => o.Price.BasePrice);
			var notBaseCount = model.Offers.Value.Count(o => o.Price.NotBase);
			var count = model.Offers.Value.Count;

			Assert.That(baseCount, Is.GreaterThan(0));
			Assert.That(notBaseCount, Is.GreaterThan(0));
			model.CurrentFilter.Value = model.Filters[1];
			Assert.AreEqual(baseCount, model.Offers.Value.Count);
			Assert.IsTrue(model.Offers.Value[0].Price.BasePrice);

			model.CurrentFilter.Value = model.Filters[0];
			Assert.AreEqual(count, model.Offers.Value.Count);

			model.CurrentFilter.Value = model.Filters[2];
			Assert.AreEqual(notBaseCount, model.Offers.Value.Count);
			Assert.IsFalse(model.Offers.Value[0].Price.BasePrice,
				model.Offers.Value[0].Price.Id.ToString());
		}

		[Test]
		public void Filter_result_empty()
		{
			var ids = model.Offers.Value.Select(o => o.Price.Id).Distinct().ToList();
			var prices = session.Query<Price>().ToList().Where(p => ids.Contains(p.Id)).ToList();

			foreach (var price in prices)
				price.BasePrice = true;
			session.Flush();

			model.CurrentFilter.Value = model.Filters[2];
		}

		[Test]
		public void Load_max_producer_costs()
		{
			//предельные цены формируются только для прайса которые был создан в текущем импорте
			//и для прайсов созданных в предыдущих импортах он не будет сформирован
			var currentPrice = session.Query<Price>().Select(p => p.Id).ToArray().Max();
			var catalogId = session.Query<Offer>().First(o => o.Price.Id == currentPrice && session.Query<Catalog>().Any(c => c.Id == o.CatalogId && c.VitallyImportant)).CatalogId;
			catalog = session.Load<Catalog>(catalogId);

			Assert.That(model.MaxProducerCosts.Value.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Load_history_orders()
		{
			session.DeleteEach<SentOrder>();

			var offer = session.Query<Offer>().First(o => o.CatalogId == catalog.Id);
			var order = new Order(offer.Price, address);
			order.TryOrder(offer, 1);
			var sentOrder = new SentOrder(order);
			session.Save(sentOrder);

			model.CurrentOffer.Value = model.Offers.Value.First(o => o.Id == offer.Id);
			scheduler.AdvanceToMs(4000);

			Assert.AreEqual(1, model.HistoryOrders.Value.Count, model.HistoryOrders.Value.Implode(l => l.Id));
			Assert.AreEqual(1, model.HistoryOrders.Value[0].Count);
			Assert.AreEqual(offer.Cost, model.CurrentOffer.Value.PrevOrderAvgCost);
			Assert.AreEqual(1, model.CurrentOffer.Value.PrevOrderAvgCount);

			model.CurrentOffer.Value = null;
			Assert.IsNull(model.HistoryOrders.Value);
			model.CurrentOffer.Value = model.Offers.Value.First();
			Assert.AreEqual(1, model.HistoryOrders.Value.Count);
		}

		[Test]
		public void Change_sort()
		{
			model.Offers.Value = new List<Offer> {
				new Offer {
					CatalogId = 1,
					ProductId = 2,
					Cost = 120
				},
				new Offer {
					CatalogId = 1,
					ProductId = 3,
					Cost = 105
				},
				new Offer {
					CatalogId = 1,
					ProductId = 3,
					Cost = 103
				},
				new Offer {
					CatalogId = 1,
					ProductId = 2,
					Cost = 90
				}
			};
			model.GroupByProduct.Value = true;
			Assert.That(model.Offers.Value.Select(o => o.Cost).Implode(), Is.EqualTo("90, 120, 103, 105"));
		}

		[Test]
		public void Load_order_history()
		{
			session.DeleteEach<SentOrder>();
			model.LoadHistoryOrders(stateless);
		}

		[Test]
		public void Load_order_history_without_address()
		{
			restore = true;
			session.DeleteEach<Order>();
			session.DeleteEach<SentOrder>();
			session.DeleteEach<Address>();

			model.LoadHistoryOrders(stateless);
		}

		[Test]
		public void Load_order_count()
		{
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Close(model);

			var renewModel = Open(new CatalogOfferViewModel(catalog));
			Assert.That(renewModel.CurrentOffer.Value.OrderCount, Is.EqualTo(1));
			Assert.That(renewModel.CurrentOffer.Value.OrderLine, Is.Not.Null);
			Assert.That(renewModel.CurrentOrder, Is.Not.Null);
		}

		[Test]
		public void Can_not_make_order_if_current_address_is_null()
		{
			shell.CurrentAddress.Value = null;
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();

			Assert.That(model.CurrentOffer.Value.OrderCount, Is.Null);
			Assert.That(model.CurrentOffer.Value.OrderLine, Is.Null);
		}

		[Test]
		public void Create_order_line_with_comment()
		{
			model.AutoCommentText = "тестовый комментарий";
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			Assert.That(model.CurrentOffer.Value.OrderLine.Comment, Is.EqualTo("тестовый комментарий"));
		}

		[Test]
		public void Reset_auto_comment()
		{
			model.AutoCommentText = "тестовый комментарий";
			model.ResetAutoComment = true;
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Assert.That(model.CurrentOffer.Value.OrderLine.Comment, Is.EqualTo("тестовый комментарий"));

			model.CurrentOffer.Value = model.Offers.Value[1];
			Assert.That(model.AutoCommentText, Is.Null);

			model.CurrentOffer.Value = model.Offers.Value[0];
			Assert.That(model.AutoCommentText, Is.EqualTo("тестовый комментарий"));
		}

		[Test]
		public void Check_prev_order_count()
		{
			catalog = session.Query<Catalog>()
				.First(c => c.HaveOffers
					&& session.Query<Offer>().Count(o => o.CatalogId == c.Id && !o.VitallyImportant && !o.Junk) >= 2
					&& !c.VitallyImportant
					&& !c.Narcotic && !c.Toxic && !c.Combined && !c.Other);
			var orderOffer = session.Query<Offer>().First(x => !x.Junk && x.CatalogId == catalog.Id);
			MakeSentOrder(orderOffer);
			var offer = model.Offers.Value.First(o => !o.Junk && o.ProductId == orderOffer.ProductId);

			model.CurrentOffer.Value = offer;
			model.CurrentOffer.Value.OrderCount = 51;
			model.OfferUpdated();
			Assert.That(model.OrderWarning.OrderWarning, Is.EqualTo("Превышение среднего заказа!"));
		}

		[Test]
		public void Reject_activate_if_offers_not_found()
		{
			catalog = session.Query<Catalog>().First(c => !c.HaveOffers);
			Assert.IsTrue(model.IsSuccessfulActivated);
			Assert.IsTrue(model.IsInitialized);
			Assert.IsFalse(model.IsActive);
			Assert.That(manager.MessageBoxes.Implode(), Is.EqualTo("Нет предложений"));
		}

		[Test]
		public void Load_promotion()
		{
			var fixture = new LocalPromotion {
				Catalog = catalog
			};
			Fixture(fixture);
			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));
			scheduler.AdvanceByMs(500);
			catalog = fixture.Promotion.Catalogs.First();
			Assert.That(model.Promotions.Promotions.Value.Count, Is.GreaterThan(0));
		}

		[Test]
		public void Show_description()
		{
			catalog = session.Query<Catalog>()
				.First(c => c.HaveOffers && c.Name.Description != null);
			var dialogs = manager.DialogOpened.Collect();
			model.ShowDescription();
			Assert.AreEqual(1, dialogs.Count);
			var description = (DocModel<ProductDescription>)dialogs[0];
			Assert.IsNotNull(description.Model);
			Assert.IsNotNull(description.Document);
		}

		[Test]
		public void Load_order_for_price()
		{
			session.DeleteEach<Order>();
			var offer = session.Query<Offer>().First();
			MakeOrder(offer);

			var catalogId = session.Query<Offer>()
				.First(o => o.Price == offer.Price && o.CatalogId != offer.CatalogId).CatalogId;
			catalog = session.Load<Catalog>(catalogId);
			model.CurrentOffer.Value = model.Offers.Value.First(o => o.Price.Id == offer.Price.Id);
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			Close(model);
			session.Clear();
			var orders = session.Query<Order>().ToArray();
			Assert.AreEqual(1, orders.Count());
			Assert.AreEqual(2, orders[0].Lines.Count);
		}

		[Test]
		public void Warn_on_lastday_orders()
		{
			Assert.IsTrue(settings.WarnIfOrderedYesterday);
			var order = MakeSentOrder(session.Query<Offer>().First(o => !o.Junk));
			order.SentOn = DateTime.Now.AddDays(-settings.CountDayForWarnOrdered);
			catalog = session.Load<Catalog>(order.Lines[0].CatalogId);

			Assert.That(model.Offers.Value.Count, Is.GreaterThan(0));
			model.CurrentOffer.Value = model.Offers.Value.First(o => o.ProductId == order.Lines[0].ProductId && !o.Junk);
			scheduler.Start();
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			model.OfferCommitted();
			Assert.That(model.OrderWarning.OrderWarning, Is.EqualTo(Util.HumanizeDaysAgo(settings.CountDayForWarnOrdered)));
		}

		[Test]
		public void Mode_PKU_warning()
		{
			var price = new Price("test1");
			model.Offers.Value = new List<Offer> {
				new Offer(price, 100) {
					Id = {
						OfferId = 1
					},
					CatalogId = 52
				},
			};
			model.CurrentCatalog.Value = new Catalog {
				Id = 52,
				Toxic = true,
			};
			model.CurrentOffer.Value = model.Offers.Value.First();
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			if (settings.ModePKU == ModePKU.Warning) {
				Assert.AreEqual("Вы заказываете препарат, подлежащий предметно-количественному учету и относящийся к ПКУ:Сильнодействующие. и ядовитые",
				model.OrderWarning.OrderWarning);
			}
			if (settings.ModePKU == ModePKU.Deny) {
				Assert.AreEqual("Заказ препаратов ПКУ запрещен. Для изменения режима заказа препаратов ПКУ," +
						" перейдите в Настройки во вкладку Визуализация и снимите запрет на заказ препаратов ПКУ",
				model.OrderWarning.OrderWarning);
				Assert.AreEqual(null, model.CurrentOffer.Value.OrderCount);
			}
		}
	}
}