using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.Acceptance;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class CatalogOfferFixture : BaseUnitFixture
	{
		private CatalogOfferViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new CatalogOfferViewModel(new Catalog("Тестовый"));
			model.User = new User();
			model.Address = new Address("тест");
			model.Addresses = new[] { model.Address };
			ScreenExtensions.TryActivate(model);
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

		[Test]
		public void Recalculate_stat_on_edit_reject()
		{
			Stat stat = null;
			bus.Listen<Stat>().Subscribe(s => stat = s);
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
			//нам нужно подождать во время отображения диалога тк результат редактирования еще не отменем
			manager.MessageOpened.Subscribe(s => {
				scheduler.AdvanceByMs(1000);
			});
			model.CurrentOffer.Value = model.Offers.Value[1];
			model.OfferCommitted();
			scheduler.AdvanceByMs(1000);
			Assert.AreEqual(0, stat.OrderLinesCount);
			Assert.IsNull(model.Offers.Value[0].OrderCount);
		}
	}
}