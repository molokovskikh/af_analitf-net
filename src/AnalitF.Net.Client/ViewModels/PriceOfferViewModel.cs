using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Documents;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class HistoryOrdersViewModel : Screen
	{
		public HistoryOrdersViewModel(Catalog catalog, Offer offer, List<SentOrderLine> lines)
		{
			Offer = offer;
			Catalog = catalog;
			Lines = lines;
			DisplayName = "Предыдущие заказы";
		}

		public Offer Offer { get; set; }

		public Catalog Catalog { get; set; }

		public List<SentOrderLine> Lines { get; set; }
	}

	public class PriceOfferViewModel : BaseOfferViewModel, IPrintable
	{
		private string[] filters = {
			"Прайс-лист (F4)",
			"Заказы (F5)",
			"Лучшие предложения (F6)",
		};

		private PriceComposedId priceId;

		public PriceOfferViewModel(PriceComposedId priceId, bool showLeaders, OfferComposedId initOfferId = null)
			: base(initOfferId)
		{
			//мы не можем принимать объект который принадлежит другой форме
			//это может вызвать исключение если сессия в которой был загружен объект будет закрыта
			//утечки памяти если текущая форма подпишется на события изменения в переданном объекте
			//между формами можно передавать только примитивные объекты
			this.priceId = priceId;

			SearchText = new NotifyValue<string>();
			Price = new NotifyValue<Price>();
			DisplayName = "Заявка поставщику";

			Filters = filters;
			CurrentFilter = new NotifyValue<string>(filters[0]);
			if (showLeaders)
				FilterLeader();

			OnCloseDisposable.Add(SearchText.Changed()
				.Throttle(Consts.SearchTimeout, Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => Search()));

			CurrentProducer.Changed()
				.Merge(CurrentFilter.Changed())
				.Subscribe(_ => Update());

			//по идее это не нужно тк обо всем должен позаботится сборщик мусора
			//но если не удалить подписку будет утечка памяти
			OnCloseDisposable.Add(this.ObservableForProperty(m => m.Price.Value.Order)
				.Subscribe(_ => NotifyOfPropertyChange("CanDeleteOrder")));
			SearchBehavior = new SearchBehavior(OnCloseDisposable, this);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<Price> Price { get; set; }
		public string[] Filters { get; set; }
		public NotifyValue<string> CurrentFilter { get; set; }

		public bool CanDeleteOrder
		{
			get { return Price.Value.Order != null && Address != null; }
		}

		public bool CanShowHistoryOrders
		{
			get { return CurrentCatalog != null; }
		}

		public bool CanPrint
		{
			get { return true; }
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
			CurrentOffer = CurrentOffer ?? Offers.Value.FirstOrDefault();

			var haveOffers = StatelessSession.Query<Offer>().Any(o => o.Price.Id == priceId);
			if (!haveOffers) {
				Manager.Warning("Выбранный прайс-лист отсутствует");
				IsSuccessfulActivated = false;
			}
		}

		protected override void Query()
		{
			var query = StatelessSession.Query<Offer>().Where(o => o.Price.Id == priceId);
			var producer = CurrentProducer.Value;
			if (producer != Consts.AllProducerLabel) {
				query = query.Where(o => o.Producer == producer);
			}
			var filter = CurrentFilter.Value;
			if (filter == filters[2]) {
				query = query.Where(o => o.LeaderPrice.Id == priceId);
			}
			if (filter == filters[1]) {
				//если мы установили фильтр по заказанным позициям то нужно
				//выполнить сохранение
				Session.Flush();
				var addressId = Address != null ? Address.Id : 0;
				query = query.Where(o => StatelessSession.Query<OrderLine>().Count(l => l.OfferId == o.Id
					&& l.Order.Address.Id == addressId
					&& !l.Order.Frozen
					&& l.Order.Price.Id == priceId) > 0);
			}
			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (!String.IsNullOrEmpty(term)) {
				query = query.Where(o => o.ProductSynonym.Contains(term));
			}

			Offers.Value = query
				.Fetch(o => o.Price)
				.Fetch(o => o.LeaderPrice)
				.ToList();

			Price.Value = Offers.Value.Select(o => o.Price).FirstOrDefault()
				?? StatelessSession.Get<Price>(priceId);
		}

		public NotifyValue<string> SearchText { get; set; }

		public void CancelFilter()
		{
			CurrentFilter.Value = Filters[0];
		}

		public void FilterOrdered()
		{
			CurrentFilter.Value = Filters[1];
		}

		public void FilterLeader()
		{
			CurrentFilter.Value = Filters[2];
		}

		public IResult Search()
		{
			return SearchBehavior.Search();
		}

		public IResult ClearSearch()
		{
			return SearchBehavior.ClearSearch();
		}

		public PrintResult Print()
		{
			var doc = new PriceOfferDocument(Offers.Value, Price, Address);
			return new PrintResult(DisplayName, doc);
		}

		public IResult ShowHistoryOrders()
		{
			if (!CanShowHistoryOrders)
				return null;

			LoadHistoryOrders();
			return new DialogResult(new HistoryOrdersViewModel(CurrentCatalog, CurrentOffer, HistoryOrders));
		}

		public IResult EnterOffer()
		{
			return ShowHistoryOrders();
		}

		public void DeleteOrder()
		{
			if (!CanDeleteOrder)
				return;

			if (!Confirm("Удалить весь заказ по данному прайс-листу?"))
				return;

			Address.Orders.Remove(Price.Value.Order);
			Price.Value.Order = null;
			foreach (var offer in Offers.Value)
				offer.OrderLine = null;
		}
	}
}