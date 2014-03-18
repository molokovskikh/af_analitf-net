using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using ReactiveUI;
using Message = Common.Tools.Message;

namespace AnalitF.Net.Client.ViewModels
{
	public abstract class BaseOfferViewModel : BaseScreen
	{
		private string autoCommentText;
		private bool resetAutoComment;
		private bool clearSession;
		private SimpleMRUCache cache = new SimpleMRUCache(10);
		private Catalog currentCatalog;
		private List<SentOrderLine> historyOrders;
		private OfferComposedId initOfferId;

		//тк уведомление о сохранении изменений приходит после
		//изменения текущего предложения
		protected Offer lastEditOffer;
		protected bool NeedToCalculateDiff;
		protected bool NavigateOnShowCatalog;
		//адрес доставки для текущего элемента, нужен если мы отображем элементы которые относятся к разным адресам доставки
		protected Address CurrentElementAddress;
		public Address[] Addresses = new Address[0];

		public BaseOfferViewModel(OfferComposedId initOfferId = null)
		{
			Readonly = true;
			updateOnActivate = false;

			this.initOfferId = initOfferId;

			Offers = new NotifyValue<List<Offer>>(new List<Offer>());
			CurrentOffer = new NotifyValue<Offer>();
			CurrentProducer = new NotifyValue<string>(Consts.AllProducerLabel);
			Producers = new NotifyValue<List<string>>(new List<string> { Consts.AllProducerLabel });

			CurrentOffer.Changed().Subscribe(_ => {
				if (ResetAutoComment) {
					AutoCommentText = CurrentOffer.Value != null && CurrentOffer.Value.OrderLine != null
						? CurrentOffer.Value.OrderLine.Comment
						: null;
				}
			});
			this.ObservableForProperty(m => m.CurrentOffer.Value)
				.Subscribe(_ => InvalidateHistoryOrders());

			this.ObservableForProperty(m => m.CurrentOffer.Value)
				.Subscribe(m => NotifyOfPropertyChange("CurrentOrder"));

			this.ObservableForProperty(m => m.CurrentCatalog)
				.Subscribe(_ => NotifyOfPropertyChange("CanShowCatalogWithMnnFilter"));

			Settings.Changed().Subscribe(_ => Calculate());
		}

		public PromotionPopup Promotions { get; set; }

		public InlineEditWarning OrderWarning { get; set; }

		public List<SentOrderLine> HistoryOrders
		{
			get { return historyOrders; }
			set
			{
				historyOrders = value;
				NotifyOfPropertyChange("HistoryOrders");
			}
		}

		public Catalog CurrentCatalog
		{
			get { return currentCatalog; }
			set
			{
				currentCatalog = value;
				NotifyOfPropertyChange("CurrentCatalog");
				NotifyOfPropertyChange("CanShowDescription");
			}
		}

		public NotifyValue<List<string>> Producers { get; set; }

		public NotifyValue<string> CurrentProducer { get; set; }

		[Export]
		public NotifyValue<List<Offer>> Offers { get; set; }

		public NotifyValue<Offer> CurrentOffer { get; set; }

		public Order CurrentOrder
		{
			get
			{
				if (CurrentOffer.Value == null)
					return null;
				if (CurrentOffer.Value.OrderLine == null)
					return null;
				return CurrentOffer.Value.OrderLine.Order;
			}
		}

		public bool CanShowCatalogWithMnnFilter
		{
			get { return CurrentCatalog != null && CurrentCatalog.Name.Mnn != null; }
		}

		public bool CanShowDescription
		{
			get
			{
				return CurrentCatalog != null
					&& CurrentCatalog.Name.Description != null;
			}
		}

		//фактический адрес доставки для которого нужно формировать заявки
		protected Address ActualAddress
		{
			get { return CurrentElementAddress ?? Address; }
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (StatelessSession != null) {
				Promotions = new PromotionPopup(StatelessSession, Shell.Config);
				this.ObservableForProperty(m => m.CurrentCatalog, skipInitial: false)
					.Subscribe(c => Promotions.Activate(c.Value == null ? null : c.Value.Name));

				Addresses = Session.Query<Address>().ToArray();
			}

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			this.ObservableForProperty(m => m.CurrentOffer.Value)
				.Where(o => o != null)
				.Throttle(Consts.LoadOrderHistoryTimeout, UiScheduler)
				.Subscribe(_ => LoadHistoryOrders(), CloseCancellation.Token);

			this.ObservableForProperty(m => m.CurrentOffer.Value)
#if !DEBUG
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
#endif
				.Subscribe(_ => {
					if (Session != null) {
						if (CurrentOffer.Value != null && (currentCatalog == null || CurrentCatalog.Id != CurrentOffer.Value.CatalogId))
							CurrentCatalog = Session.Load<Catalog>(CurrentOffer.Value.CatalogId);
					}
				}, CloseCancellation.Token);

			var observable = this.ObservableForProperty(m => m.CurrentOffer.Value.OrderCount)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));

			OnCloseDisposable.Add(Bus.RegisterMessageSource(observable));
			Bus.Listen<string>("db")
				.Where(m => m == "Changed")
				.Subscribe(_ => clearSession = true, CloseCancellation.Token);
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DocModel<ProductDescription>(CurrentCatalog.Name.Description.Id));
		}

		public void ShowCatalogWithMnnFilter()
		{
			if (!CanShowCatalogWithMnnFilter)
				return;

			Shell.Navigate(new CatalogViewModel {
				FiltredMnn = CurrentCatalog.Name.Mnn
			});
		}

		public void ShowCatalog()
		{
			if (CurrentCatalog == null)
				return;

			var offerViewModel = new CatalogOfferViewModel(CurrentCatalog,
				CurrentOffer.Value != null ? CurrentOffer.Value.Id : null);

			if (NavigateOnShowCatalog) {
				Shell.Navigate(offerViewModel);
			}
			else {
				var catalogViewModel = new CatalogViewModel();
				catalogViewModel.CurrentCatalog = CurrentCatalog;

				Shell.NavigateAndReset(catalogViewModel, offerViewModel);
			}
		}

		public static List<Offer> SortByMinCostInGroup<T>(List<Offer> offer, Func<Offer, T> key, bool setGroupKey = true)
		{
			var lookup = offer.GroupBy(key)
				.ToDictionary(g => g.Key, g => g.Min(o => o.ResultCost));

			var offers = offer.OrderBy(o => Tuple.Create(lookup[key(o)], o.ResultCost)).ToList();

			var indexes = lookup.OrderBy(k => k.Value)
				.Select((k, i) => Tuple.Create(k.Key, i))
				.ToDictionary(t => t.Item1, t => t.Item2);

			offers.Each(o => {
				o.IsGrouped = setGroupKey && indexes[key(o)] % 2 > 0;
			});

			return offers;
		}

		private void CalculateRetailCost()
		{
			foreach (var offer in Offers.Value)
				offer.CalculateRetailCost(Settings.Value.Markups, User);
		}

		public virtual void OfferUpdated()
		{
			if (CurrentOffer.Value == null)
				return;

			lastEditOffer = CurrentOffer.Value;
			LoadStat();
			ShowValidationError(CurrentOffer.Value.UpdateOrderLine(ActualAddress, Settings.Value, Confirm, AutoCommentText));
		}

		public void OfferCommitted()
		{
			if (lastEditOffer == null)
				return;

			ShowValidationError(lastEditOffer.SaveOrderLine(ActualAddress, Settings.Value, Confirm, AutoCommentText));
		}

		protected void ShowValidationError(List<Message> messages)
		{
			OrderWarning.Show(messages);

			//если человек ушел с этой позиции а мы откатываем значение то нужно вернуть его к этой позиции что бы он
			//мог ввести корректное значение
			var errors = messages.Where(m => m.IsError);
			if (errors.Any()) {
				if (CurrentOffer.Value == null || CurrentOffer.Value.Id != lastEditOffer.Id) {
					CurrentOffer.Value = lastEditOffer;
				}
			}
		}

		protected void Calculate()
		{
			if (NeedToCalculateDiff)
				CalculateDiff();

			CalculateRetailCost();
		}

		private void CalculateDiff()
		{
			decimal baseCost = 0;
			var diffCalcMode = Settings.Value.DiffCalcMode;
			var offers = Offers.Value;
			if (diffCalcMode == DiffCalcMode.MinCost)
				baseCost = offers.Select(o => o.ResultCost).MinOrDefault();
			else if (diffCalcMode == DiffCalcMode.MinBaseCost)
				baseCost = offers.Where(o => o.Price.BasePrice).Select(o => o.ResultCost).MinOrDefault();

			foreach (var offer in offers) {
				offer.CalculateDiff(baseCost);
				if (diffCalcMode == DiffCalcMode.PrevOffer)
					baseCost = offer.ResultCost;
			}
		}

		public string AutoCommentText
		{
			get
			{
				return autoCommentText;
			}
			set
			{
				autoCommentText = value;
				NotifyOfPropertyChange("AutoCommentText");
			}
		}

		public bool ResetAutoComment
		{
			get
			{
				return resetAutoComment;
			}
			set
			{
				resetAutoComment = value;
				NotifyOfPropertyChange("ResetAutoComment");
			}
		}

		protected void LoadOrderItems()
		{
			foreach (var offer in Offers.Value) {
				offer.Price.Order = null;
				offer.OrderLine = null;
			}

			if (ActualAddress == null)
				return;

			var lines = ActualAddress.ActiveOrders().SelectMany(o => o.Lines).ToLookup(l => l.OfferId);
			foreach (var offer in Offers.Value) {
				offer.OrderLine = lines[offer.Id].FirstOrDefault();
			}
		}

		protected abstract void Query();

		protected override void OnActivate()
		{
			//если это не первичная активация и данные в базе были изменены то нужно перезагрузить сессию
			if (clearSession) {
				Session.Clear();
				RecreateSession();
				clearSession = false;
			}

			base.OnActivate();
		}

		protected virtual void RecreateSession()
		{
			Addresses = Session.Query<Address>().ToArray();
			if (Address != null)
				Address = Session.Get<Address>(Address.Id);
			if (CurrentElementAddress != null)
				CurrentElementAddress = Session.Get<Address>(CurrentElementAddress.Id);
			User = Session.Query<User>().FirstOrDefault();
			LoadOrderItems();
		}

		public override void Update()
		{
			Query();
			CurrentOffer.Value = CurrentOffer.Value ?? Offers.Value.FirstOrDefault(o => o.Id == initOfferId);
			Calculate();
			LoadOrderItems();
		}

		protected void LoadStat()
		{
			if (ActualAddress == null || CurrentOffer.Value == null || StatelessSession == null)
				return;

			if (CurrentOffer.Value.StatLoaded)
				return;

			var begin = DateTime.Now.AddMonths(-1);
			var values = StatelessSession.CreateSQLQuery(@"select avg(cost) as avgCost, avg(count) as avgCount
from SentOrderLines ol
join SentOrders o on o.Id = ol.OrderId
where o.SentOn > :begin and ol.ProductId = :productId and o.AddressId = :addressId")
				.SetParameter("begin", begin)
				.SetParameter("productId", CurrentOffer.Value.ProductId)
				.SetParameter("addressId", ActualAddress.Id)
				.UniqueResult<object[]>();
			CurrentOffer.Value.PrevOrderAvgCost = (decimal?)values[0];
			CurrentOffer.Value.PrevOrderAvgCount = (decimal?)values[1];
			CurrentOffer.Value.StatLoaded = true;
		}

		private void InvalidateHistoryOrders()
		{
			HistoryOrders = (List<SentOrderLine>)cache[HistoryOrdersCacheKey()];
		}

		public void LoadHistoryOrders()
		{
			if (ActualAddress == null || CurrentOffer.Value == null || StatelessSession == null)
				return;

			var key = HistoryOrdersCacheKey();
			HistoryOrders = Cache(key, cache, k => {
				IQueryable<SentOrderLine> query = StatelessSession.Query<SentOrderLine>()
					.OrderByDescending(o => o.Order.SentOn);
				//ошибка в nhibernate, если .Where(o => o.Order.Address == Address)
				//переместить в общий блок то первый
				//where применяться не будет
				var addressId = ActualAddress.Id;
				if (Settings.Value.GroupByProduct) {
					var productId = CurrentOffer.Value.ProductId;
					query = query.Where(o => o.ProductId == productId)
						.Where(o => o.Order.Address.Id == addressId);
				}
				else {
					var catalogId = CurrentOffer.Value.CatalogId;
					query = query.Where(o => o.CatalogId == catalogId)
						.Where(o => o.Order.Address.Id == addressId);
				}
				return query
					.Fetch(l => l.Order)
					.ThenFetch(o => o.Price)
					.Take(20)
					.ToList();
			});

			LoadStat();
		}

		public T Cache<T, TKey>(TKey key, SimpleMRUCache cache, Func<TKey, T> select)
		{
			var cached = (T)cache[key];
			if (!Equals(cached, default(T))) {
				return cached;
			}
			cached = select(key);
			cache.Put(key, cached);
			return cached;
		}

		private object HistoryOrdersCacheKey()
		{
			if (CurrentOffer.Value == null || ActualAddress == null)
				return 0;

			if (Settings.Value.GroupByProduct)
				return CurrentOffer.Value.ProductId;
			else
				return CurrentOffer.Value.CatalogId;
		}
	}
}
