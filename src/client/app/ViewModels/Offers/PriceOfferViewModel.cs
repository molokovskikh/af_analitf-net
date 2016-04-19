using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class PriceOfferViewModel : BaseOfferViewModel, IPrintable
	{
		private string[] filters = {
			"Прайс-лист (F4)",
			"Заказы (F5)",
			"Лучшие предложения (F6)",
		};

		private PriceComposedId priceId;
		public List<Offer> PriceOffers = new List<Offer>();

		public PriceOfferViewModel(PriceComposedId priceId, bool showLeaders, OfferComposedId initOfferId = null)
			: base(initOfferId)
		{
			//мы не можем принимать объект который принадлежит другой форме
			//это может вызвать исключение если сессия в которой был загружен объект будет закрыта
			//утечки памяти если текущая форма подпишется на события изменения в переданном объекте
			//между формами можно передавать только примитивные объекты
			this.priceId = priceId;

			Price = new NotifyValue<Price>();
			DisplayName = "Заявка поставщику";

			Filters = filters;
			CurrentFilter = new NotifyValue<string>(filters[0]);
			if (showLeaders)
				FilterLeader();

			//по идее это не нужно тк обо всем должен позаботится сборщик мусора
			//но если не удалить подписку будет утечка памяти
			OnCloseDisposable.Add(this.ObservableForProperty(m => m.Price.Value.Order)
				.Subscribe(_ => NotifyOfPropertyChange("CanDeleteOrder")));
			SearchBehavior = new SearchBehavior(this);
			IsLoading = new NotifyValue<bool>(true);

			CurrentProducer.Cast<object>()
				.Merge(CurrentFilter.Cast<object>())
				.Merge(SearchBehavior.ActiveSearchTerm.Cast<object>())
				.Subscribe(_ => Filter());
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<Price> Price { get; set; }
		public string[] Filters { get; set; }
		public NotifyValue<string> CurrentFilter { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }

		public bool CanDeleteOrder => Price.Value.Order != null && Address != null;

		public bool CanShowHistoryOrders => CurrentCatalog.Value != null;

		public bool CanPrint => User.CanPrint<PriceOfferDocument>();

		protected override void OnActivate()
		{
			base.OnActivate();

			if (StatelessSession != null)
				Price.Value = StatelessSession.Get<Price>(priceId);
			if (Promotions != null)
				Promotions.FilterBySupplierId = Price.Value.SupplierId;

			RxQuery(s => {
					return s.Query<Offer>().Where(o => o.Price.Id == priceId)
						.Fetch(o => o.Price)
						.Fetch(o => o.LeaderPrice)
						.ToList();
				})
				.ObserveOn(UiScheduler)
				.CatchSubscribe(BindOffers, CloseCancellation);
		}

		public void BindOffers(List<Offer> offers)
		{
			Calculate(offers);
			LoadOrderItems(offers);
			PriceOffers = offers;
			FillProducerFilter(PriceOffers);
			Filter();
			SelectOffer();
			Price.Value = PriceOffers.Select(o => o.Price).FirstOrDefault()
				?? Price.Value;
			IsLoading.Value = false;
			if (PriceOffers.Count == 0) {
				ResultsSink.OnNext(MessageResult.Warn("Выбранный прайс-лист отсутствует"));
				TryClose();
			}
		}

		public void Filter()
		{
			var filter = CurrentFilter.Value;
			IEnumerable<Offer> items = PriceOffers;
			if (filter == filters[2]) {
				items = items.Where(o => o.Leader);
			}
			if (filter == filters[1]) {
				items = items.Where(o => o.OrderCount > 0);
			}

			ProducerFilterStateGet(items.ToList());

			if (CurrentProducer.Value != null && CurrentProducer.Value.Id > 0) {
				var id = CurrentProducer.Value.Id;
				items = items.Where(o => o.ProducerId == id);
			}
			var term = SearchBehavior.ActiveSearchTerm.Value;
			if (!String.IsNullOrEmpty(term)) {
				items = items.Where(o => o.ProductSynonym.IndexOf(term ?? "", StringComparison.CurrentCultureIgnoreCase) >= 0);
			}
			Offers.Value = items.OrderBy(o => o.ProductSynonym).ToList();
		}

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

		public PrintResult Print()
		{
			var offers = GetPrintableOffers();
			var doc = new PriceOfferDocument(offers, Price, Address);
			return new PrintResult(DisplayName, doc);
		}

		public IResult ShowHistoryOrders()
		{
			if (!CanShowHistoryOrders)
				return null;

			LoadHistoryOrders(StatelessSession);
			return new DialogResult(new HistoryOrdersViewModel(CurrentCatalog.Value,
				CurrentOffer.Value, HistoryOrders), fixedSize: true);
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

			if (CurrentFilter.Value == filters[1]) {
				Filter();
			}
		}

		public override void TryClose()
		{
			ProducerFilterStateSet();
			base.TryClose();
		}

		public override void OfferUpdated()
		{
			base.OfferUpdated();

			if (LastEditOffer.Value == null)
				return;
			if (LastEditOffer.Value.OrderLine == null && CurrentFilter.Value == filters[1]) {
				Filter();
			}
		}

		protected override void LoadOrderItems()
		{
			LoadOrderItems(PriceOffers);
		}
	}
}