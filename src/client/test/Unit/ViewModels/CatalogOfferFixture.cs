using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Offers;
using Caliburn.Micro;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Unit.ViewModels
{
	[TestFixture]
	public class CatalogOfferFixture : BaseUnitFixture
	{
		private CatalogOfferViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new CatalogOfferViewModel(new Catalog("Тестовый"));
			model.CatalogOffers = new List<Offer> {
				new Offer(new Price("test1") { RegionName = "Воронеж" }, 100) {
					Id = {
						OfferId = 1
					}
				},
				new Offer(new Price("test2") { RegionName = "Воронеж" }, 150) {
					Id = {
						OfferId = 2
					},
				}
			};
			model.UpdateFilters();
			Activate(model);
		}

		[Test]
		public void Recalculate_on_offer_changed()
		{
			model.Offers.Value = new List<Offer> {
				new Offer(new Price("test1"), 100) {
					Id = {
						OfferId = 1
					}
				},
				new Offer(new Price("test2"), 150) {
					Id = {
						OfferId = 2
					}
				}
			};
			model.CurrentOffer.Value = model.Offers.Value[0];
			Assert.AreEqual(model.RetailMarkup.Value, 20);
			Assert.AreEqual(model.RetailCost.Value, 120);
			model.CurrentOffer.Value = model.Offers.Value[1];
			Assert.AreEqual(model.RetailCost.Value, 180);
		}

		/// <summary>
		/// Проверяем округление цены.
		/// </summary>
		[Test]
		public void Recalculate_on_rounding_changed()
		{
			//Arrange
			decimal expectedNotRound = new decimal(1.65);
			decimal expectedRound010 = new decimal(1.60);
			decimal expectedRound050 = new decimal(1.50);
			decimal expectedRound100 = new decimal(1.00);
			model.Offers.Value = new List<Offer> {
				new Offer(new Price("test1"), expectedNotRound) {
					Id = {
						OfferId = 1
					}
				}
			};
			model.CurrentOffer.Value = model.Offers.Value[0];
			model.RetailMarkup.Value = 0;

			//Act
			var costNotRound = model.RetailCost.Value;
			model.Rounding.Value = Rounding.To0_10;
			var costRound010 = model.RetailCost.Value;
			model.Rounding.Value = Rounding.To0_50;
			var costRound050 = model.RetailCost.Value;
			model.Rounding.Value = Rounding.To1_00;
			var costRound100 = model.RetailCost.Value;

			//Assert
			Assert.AreEqual(expectedNotRound, costNotRound);
			Assert.AreEqual(expectedRound010, costRound010);
			Assert.AreEqual(expectedRound050, costRound050);
			Assert.AreEqual(expectedRound100, costRound100);
		}

		[Test]
		public void Recalculate_stat_on_edit_reject()
		{
			var stat = bus.Listen<Stat>().ToValue();
			model.Offers.Value = new List<Offer> {
				new Offer(new Price("test1"), 100) {
					Id = {
						OfferId = 1
					},
					BuyingMatrixType = BuyingMatrixStatus.Warning
				},
				new Offer(new Price("test2"), 150) {
					Id = {
						OfferId = 2
					}
				}
			};

			manager.DefaultQuestsionResult = MessageBoxResult.No;
			model.CurrentOffer.Value = model.Offers.Value[0];
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			//симулируем переход на другую строку и подтверждение редактирования
			//нам нужно подождать во время отображения диалога тк результат редактирования еще не отменен
			manager.MessageOpened.Subscribe(s => {
				scheduler.AdvanceByMs(1000);
			});
			model.CurrentOffer.Value = model.Offers.Value[1];
			model.OfferCommitted();
			scheduler.AdvanceByMs(1000);
			Assert.AreEqual(0, stat.Value.OrderLinesCount);
			Assert.IsNull(model.Offers.Value[0].OrderCount);
		}

		[Test]
		public void Save_auto_comment_for_session()
		{
			model.AutoCommentText = "test";
			Assert.IsTrue(model.IsActive);
			Close(model);
			Assert.IsFalse(model.IsActive);
			model = new CatalogOfferViewModel(new Catalog("Тестовый"));
			Activate(model);
			Assert.AreEqual("test", model.AutoCommentText);
		}

		[Test]
		public void Do_not_order_from_forbidden_prices()
		{
			var price = new Price("test1");
			model.Offers.Value = new List<Offer> {
				new Offer(price, 100) {
					Id = {
						OfferId = 1
					},
				},
			};
			price.IsOrderDisabled = true;
			model.CurrentOffer.Value = model.Offers.Value.First();
			model.CurrentOffer.Value.OrderCount = 1;
			model.OfferUpdated();
			Assert.IsNull(model.CurrentOffer.Value.OrderCount);
			Assert.IsNull(model.CurrentOffer.Value.OrderLine);
		}

		[Test]
		public void Save_region_and_type()
		{
			Assert.AreEqual("Воронеж", model.Regions.Value[1]);
			model.CurrentRegion.Value = "Воронеж";
			Assert.AreEqual("Основные", model.Filters[1]);
			model.CurrentFilter.Value = model.Filters[1];
			Close(model);

			model = new CatalogOfferViewModel(new Catalog("Тестовый"));
			Activate(model);
			Assert.AreEqual("Воронеж", model.CurrentRegion.Value);
			Assert.AreEqual("Основные", model.CurrentFilter.Value);
		}

		[Test]
		public void Warn_on_toxic()
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
			Assert.AreEqual("Вы заказываете препарат, подлежащий предметно-количественному учету и относящийся к ПКУ:Сильнодействующие. и ядовитые",
				model.OrderWarning.OrderWarning);
		}

		[Test]
		public void Delete_value_confirm()
		{
			var stat = bus.Listen<Stat>().ToValue();
			model.Offers.Value = new List<Offer> {
				new Offer(new Price("test1"), 100) {
					Id = {
						OfferId = 1
					},
					BuyingMatrixType = BuyingMatrixStatus.Warning
				},
				new Offer(new Price("test2"), 150) {
					Id = {
						OfferId = 2
					}
				}
			};
			manager.DefaultQuestsionResult = MessageBoxResult.Yes;
			model.CurrentOffer.Value = model.Offers.Value[0];
			model.CurrentOffer.Value.OrderCount = 1;
			model.Delete();
			model.OfferUpdated();
			Assert.AreEqual(null, model.CurrentOffer.Value.OrderCount);
			manager.DefaultQuestsionResult = MessageBoxResult.No;
			model.CurrentOffer.Value = model.Offers.Value[0];
			model.CurrentOffer.Value.OrderCount = 1;
			model.Delete();
			model.OfferUpdated();
			Assert.AreEqual(1, model.CurrentOffer.Value.OrderCount);
		}
	}
}