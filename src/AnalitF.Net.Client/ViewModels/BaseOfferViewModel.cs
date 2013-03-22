using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;
using Message = Common.Tools.Message;

namespace AnalitF.Net.Client.ViewModels
{
	public abstract class BaseOfferViewModel : BaseScreen
	{
		private Catalog currentCatalog;
		protected List<string> producers;
		protected List<Offer> offers;
		protected string currentProducer;
		private Offer currentOffer;
		protected List<MarkupConfig> markups = new List<MarkupConfig>();
		private List<SentOrderLine> historyOrders;
		//тк уведомление о сохранении изменений приходит после
		//изменения текущего предложения
		private Offer lastEditOffer;

		protected bool NeedToCalculateDiff;

		protected string autoCommentText;
		protected bool resetAutoComment;

		protected bool NavigateOnShowCatalog;

		private object orderHistoryCacheKey;

		public BaseOfferViewModel()
		{
			markups = Session.Query<MarkupConfig>().ToList();

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => InvalidateHistoryOrders());

			this.ObservableForProperty(m => m.CurrentOffer)
				.Where(o => o != null)
				.Throttle(Consts.LoadOrderHistoryTimeout, Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => LoadHistoryOrders());

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(m => NotifyOfPropertyChange("CurrentOrder"));

			this.ObservableForProperty(m => m.CurrentCatalog)
				.Subscribe(_ => NotifyOfPropertyChange("CanShowCatalogWithMnnFilter"));

			var observable = this.ObservableForProperty(m => m.CurrentOffer.OrderCount)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));
			Bus.RegisterMessageSource(observable);
		}

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

		public List<string> Producers
		{
			get { return producers; }
			set
			{
				producers = value;
				NotifyOfPropertyChange("Producers");
			}
		}

		[Export]
		public List<Offer> Offers
		{
			get { return offers; }
			set
			{
				offers = value;
				NotifyOfPropertyChange("Offers");
			}
		}

		public string CurrentProducer
		{
			get { return currentProducer; }
			set
			{
				currentProducer = value;
				NotifyOfPropertyChange("CurrentProducer");
			}
		}

		public Offer CurrentOffer
		{
			get { return currentOffer; }
			set
			{
				currentOffer = value;
				if (ResetAutoComment) {
					var commentText = currentOffer.OrderLine != null ? currentOffer.OrderLine.Comment : null;
					AutoCommentText = commentText;
				}
				NotifyOfPropertyChange("CurrentOffer");
				if (currentOffer != null && (currentCatalog == null || CurrentCatalog.Id != currentOffer.CatalogId))
					CurrentCatalog = Session.Load<Catalog>(currentOffer.CatalogId);
			}
		}

		public Order CurrentOrder
		{
			get
			{
				if (CurrentOffer == null)
					return null;
				if (CurrentOffer.OrderLine == null)
					return null;
				return CurrentOffer.OrderLine.Order;
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

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DescriptionViewModel(CurrentCatalog.Name.Description));
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
			if (CurrentOffer == null)
				return;

			var offerViewModel = new CatalogOfferViewModel(CurrentCatalog);
			offerViewModel.CurrentOffer = CurrentOffer;

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
				.ToDictionary(g => g.Key, g => g.Min(o => o.Cost));

			var offers = offer.OrderBy(o => Tuple.Create(lookup[key(o)], o.Cost)).ToList();

			var indexes = lookup.OrderBy(k => k.Value)
				.Select((k, i) => Tuple.Create(k.Key, i))
				.ToDictionary(t => t.Item1, t => t.Item2);

			offers.Each(o => {
				o.SortKeyGroup = setGroupKey ? indexes[key(o)] % 2 : 0;
			});

			return offers;
		}

		protected void UpdateProducers()
		{
			var offerProducers = Offers.Select(o => o.Producer).Distinct().OrderBy(p => p);
			Producers = new[] { Consts.AllProducerLabel }.Concat(offerProducers).ToList();
		}

		private void CalculateRetailCost()
		{
			foreach (var offer in Offers)
				offer.CalculateRetailCost(markups);
		}

		public void OfferUpdated()
		{
			if (CurrentOffer == null)
				return;

			lastEditOffer = CurrentOffer;
			LoadStat();
			ShowValidationError(CurrentOffer.UpdateOrderLine(Address, Settings, AutoCommentText));
		}

		public void OfferCommitted()
		{
			if (lastEditOffer == null)
				return;

			ShowValidationError(lastEditOffer.SaveOrderLine(Address, Settings, AutoCommentText));
		}

		protected void ShowValidationError(List<Message> messages)
		{
			OrderWarning.Show(messages);

			//если человек ушел с этой позиции а мы откатываем значение то нужно вернуть его к этой позиции что бы он
			//мог ввести корректное значение
			var errors = messages.Where(m => m.IsError);
			if (errors.Any()) {
				if (CurrentOffer == null || CurrentOffer.Id != lastEditOffer.Id) {
					CurrentOffer = lastEditOffer;
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
			if (Settings.DiffCalcMode == DiffCalcMode.MinCost)
				baseCost = Offers.Select(o => o.Cost).MinOrDefault();
			else if (Settings.DiffCalcMode == DiffCalcMode.MinBaseCost)
				baseCost = Offers.Where(o => o.Price.BasePrice).Select(o => o.Cost).MinOrDefault();

			foreach (var offer in Offers) {
				offer.CalculateDiff(baseCost);
				if (Settings.DiffCalcMode == DiffCalcMode.PrevOffer)
					baseCost = offer.Cost;
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
			if (Address == null)
				return;

			var lines = Address.Orders.Where(o => !o.Frozen).SelectMany(o => o.Lines).ToList();

			foreach (var offer in Offers) {
				var line = lines.FirstOrDefault(l => l.OfferId == offer.Id);
				if (line != null) {
					offer.AttachOrderLine(line);
				}
			}
		}

		protected abstract void Query();

		public void Update()
		{
			Query();
			Calculate();
			LoadOrderItems();
		}

		protected void LoadStat()
		{
			if (Address == null || CurrentOffer == null)
				return;

			if (CurrentOffer.StatLoaded)
				return;

			var begin = DateTime.Now.AddMonths(-1);
			var values = StatelessSession.CreateSQLQuery(@"select avg(cost) as avgCost, avg(count) as avgCount
from SentOrderLines ol
join SentOrders o on o.Id = ol.OrderId
where o.SentOn > :begin and ol.ProductId = :productId and o.AddressId = :addressId")
				.SetParameter("begin", begin)
				.SetParameter("productId", CurrentOffer.ProductId)
				.SetParameter("addressId", Address.Id)
				.UniqueResult<object[]>();
			CurrentOffer.PrevOrderAvgCost = (decimal?)values[0];
			CurrentOffer.PrevOrderAvgCount = (decimal?)values[1];
			CurrentOffer.StatLoaded = true;
		}

		private void InvalidateHistoryOrders()
		{
			if (Equals(orderHistoryCacheKey, HistoryOrdersCacheKey()))
				return;

			HistoryOrders = new List<SentOrderLine>();
		}

		public void LoadHistoryOrders()
		{
			if (CurrentOffer == null || Address == null)
				return;

			var currentCacheKey = HistoryOrdersCacheKey();

			if (Equals(currentCacheKey, orderHistoryCacheKey))
				return;

			IQueryable<SentOrderLine> query = StatelessSession.Query<SentOrderLine>()
				.OrderByDescending(o => o.Order.SentOn);
			//ошибка в nhibernate, если .Where(o => o.Order.Address == Address)
			//переместить в общий блок то первый
			//where применяться не будет
			if (Settings.GroupByProduct) {
				query = query.Where(o => o.CatalogId == CurrentOffer.CatalogId)
					.Where(o => o.Order.Address == Address);
			}
			else {
				query = query.Where(o => o.ProductId == CurrentOffer.ProductId)
					.Where(o => o.Order.Address == Address);
			}
			HistoryOrders = query
				.Fetch(l => l.Order)
				.ThenFetch(o => o.Price)
				.Take(20)
				.ToList();

			orderHistoryCacheKey = currentCacheKey;
			LoadStat();
		}

		private object HistoryOrdersCacheKey()
		{
			if (CurrentOffer == null || Address == null)
				return null;

			var currentCacheKey = CurrentOffer.CatalogId;
			if (!Settings.GroupByProduct)
				currentCacheKey = CurrentOffer.ProductId;
			return currentCacheKey;
		}
	}
}
