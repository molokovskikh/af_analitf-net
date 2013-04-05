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
		private string[] filters = new[] {
			"Прайс-лист (F4)",
			"Заказы (F5)",
			"Лучшие предложения (F6)",
		};

		private string currentFilter;
		private string activeSearchTerm;
		private PriceComposedId priceId;

		public PriceOfferViewModel(PriceComposedId priceId, bool showLeaders)
		{
			//мы не можем принимать объект который принадлежит другой форме
			//это может вызвать исключение если сессия в которой был загруже объект будет закрыта
			//утечки памяти если текущая форма подпишется на события изменения в переданном объекте
			//между формами можно передавать только примитивные объекты
			this.priceId = priceId;

			SearchText = new NotifyValue<string>();
			Price = new NotifyValue<Price>();
			DisplayName = "Заявка поставщику";

			Filters = filters;
			currentProducer = Consts.AllProducerLabel;
			currentFilter = filters[0];
			if (showLeaders)
				currentFilter = filters[2];

			this.ObservableForProperty(m => m.CurrentFilter)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.Subscribe(e => Update());

			//по идее это не нужно тк обо всем должен позаботится сборщик мусора
			//но если не удалить подписку будет утечка памяти
			OnCloseDisposable.Add(this.ObservableForProperty(m => m.Price.Value.Order)
				.Subscribe(_ => NotifyOfPropertyChange("CanDeleteOrder")));
		}

		public NotifyValue<Price> Price { get; set; }

		public string[] Filters { get; set; }

		public string ActiveSearchTerm
		{
			get { return activeSearchTerm; }
			set
			{
				activeSearchTerm = value;
				NotifyOfPropertyChange("ActiveSearchTerm");
			}
		}

		public string CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				NotifyOfPropertyChange("CurrentFilter");
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
			UpdateProducers();

			var haveOffers = StatelessSession.Query<Offer>().Any(o => o.Price.Id == priceId);
			if (!haveOffers) {
				Manager.Warning("Выбранный прайс-лист отсутствует");
				IsSuccessfulActivated = false;
			}
		}

		protected override void Query()
		{
			var query = StatelessSession.Query<Offer>().Where(o => o.Price.Id == priceId);
			if (CurrentProducer != Consts.AllProducerLabel) {
				query = query.Where(o => o.Producer == CurrentProducer);
			}
			if (CurrentFilter == filters[2]) {
				query = query.Where(o => o.LeaderPrice.Id == priceId);
			}
			if (currentFilter == filters[1]) {
				//если мы установили фильтр по заказанным позициям то нужно
				//выполнить сохранение
				Session.Flush();
				var addressId = Address != null ? Address.Id : 0;
				query = query.Where(o => StatelessSession.Query<OrderLine>().Count(l => l.OfferId == o.Id
					&& l.Order.Address.Id == addressId
					&& !l.Order.Frozen
					&& l.Order.Price.Id == priceId) > 0);
			}
			if (!String.IsNullOrEmpty(ActiveSearchTerm)) {
				query = query.Where(o => o.ProductSynonym.Contains(ActiveSearchTerm));
			}

			Offers = query
				.Fetch(o => o.Price)
				.Fetch(o => o.LeaderPrice)
				.ToList();
			CurrentOffer = offers.FirstOrDefault();

			if (CurrentOffer != null)
				Price.Value = CurrentOffer.Price;

			if (Price.Value == null)
				Price.Value = StatelessSession.Get<Price>(priceId);
		}

		public NotifyValue<string> SearchText { get; set; }

		public void CancelFilter()
		{
			CurrentFilter = Filters[0];
		}

		public void FilterOrdered()
		{
			CurrentFilter = Filters[1];
		}

		public void FilterLeader()
		{
			CurrentFilter = Filters[2];
		}

		public IResult Search()
		{
			if (string.IsNullOrEmpty(SearchText.Value) || SearchText.Value.Length < 3) {
				return HandledResult.Skip();
			}

			ActiveSearchTerm = SearchText;
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}

		public IResult ClearSearch()
		{
			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return HandledResult.Skip();

			ActiveSearchTerm = "";
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			var doc = new PriceOfferDocument(offers, Price, Address).BuildDocument();
			return new PrintResult(doc, DisplayName);
		}

		public bool CanShowHistoryOrders
		{
			get { return CurrentCatalog != null; }
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

		public bool CanDeleteOrder
		{
			get
			{
				return Price.Value.Order != null && Address != null;
			}
		}

		public void DeleteOrder()
		{
			if (!CanDeleteOrder)
				return;

			if (!Confirm("Удалить весь заказ по данному прайс-листу?"))
				return;

			Address.Orders.Remove(Price.Value.Order);
			Price.Value.Order = null;
			foreach (var offer in Offers) {
				offer.OrderLine = null;
			}
		}
	}
}