﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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
using NHibernate.Util;
using ReactiveUI;
using Message = Common.Tools.Message;

namespace AnalitF.Net.Client.ViewModels
{
	public abstract class BaseOfferViewModel : BaseScreen
	{
		private SimpleMRUCache cache = new SimpleMRUCache(10);
		private Catalog currentCatalog;
		private List<SentOrderLine> historyOrders;
		//тк уведомление о сохранении изменений приходит после
		//изменения текущего предложения
		private Offer lastEditOffer;
		private Offer currentOffer;

		private string autoCommentText;
		private bool resetAutoComment;
		private OfferComposedId initOfferId;

		protected bool NeedToCalculateDiff;
		protected bool NavigateOnShowCatalog;

		public BaseOfferViewModel(OfferComposedId initOfferId = null)
		{
			Readonly = true;
			updateOnActivate = false;

			this.initOfferId = initOfferId;

			CurrentProducer = new NotifyValue<string>(Consts.AllProducerLabel);
			Offers = new NotifyValue<List<Offer>>(new List<Offer>());
			Producers = new NotifyValue<List<string>>(
				new List<string>(),
				() => new[] { Consts.AllProducerLabel }
					.Concat(Offers.Value.Select(o => o.Producer).Distinct().OrderBy(p => p))
					.ToList(),
				Offers);

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(_ => InvalidateHistoryOrders());

			this.ObservableForProperty(m => m.CurrentOffer)
				.Subscribe(m => NotifyOfPropertyChange("CurrentOrder"));

			this.ObservableForProperty(m => m.CurrentCatalog)
				.Subscribe(_ => NotifyOfPropertyChange("CanShowCatalogWithMnnFilter"));

			Settings.Changed().Subscribe(_ => Calculate());
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			this.ObservableForProperty(m => m.CurrentOffer)
				.Where(o => o != null)
				.Throttle(Consts.LoadOrderHistoryTimeout, Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => LoadHistoryOrders());

			this.ObservableForProperty(m => m.CurrentOffer)
#if !DEBUG
				.Throttle(Consts.ScrollLoadTimeout)
				.ObserveOn(UiScheduler)
#endif
				.Subscribe(_ => {
					if (currentOffer != null && (currentCatalog == null || CurrentCatalog.Id != currentOffer.CatalogId))
						CurrentCatalog = Session.Load<Catalog>(currentOffer.CatalogId);
				});

			var observable = this.ObservableForProperty(m => m.CurrentOffer.OrderCount)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));

			OnCloseDisposable.Add(Bus.RegisterMessageSource(observable));
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

		public NotifyValue<List<string>> Producers { get; set; }

		public NotifyValue<string> CurrentProducer { get; set; }

		[Export]
		public NotifyValue<List<Offer>> Offers { get; set; }

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

			var offerViewModel = new CatalogOfferViewModel(CurrentCatalog,
				CurrentOffer == null ? null : CurrentOffer.Id);

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


		private void CalculateRetailCost()
		{
			foreach (var offer in Offers.Value)
				offer.CalculateRetailCost(Settings.Value.Markups);
		}

		public void OfferUpdated()
		{
			if (CurrentOffer == null)
				return;

			lastEditOffer = CurrentOffer;
			LoadStat();
			ShowValidationError(CurrentOffer.UpdateOrderLine(Address, Settings.Value, AutoCommentText));
		}

		public void OfferCommitted()
		{
			if (lastEditOffer == null)
				return;

			ShowValidationError(lastEditOffer.SaveOrderLine(Address, Settings.Value, AutoCommentText));
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
			var diffCalcMode = Settings.Value.DiffCalcMode;
			var offers = Offers.Value;
			if (diffCalcMode == DiffCalcMode.MinCost)
				baseCost = offers.Select(o => o.Cost).MinOrDefault();
			else if (diffCalcMode == DiffCalcMode.MinBaseCost)
				baseCost = offers.Where(o => o.Price.BasePrice).Select(o => o.Cost).MinOrDefault();

			foreach (var offer in offers) {
				offer.CalculateDiff(baseCost);
				if (diffCalcMode == DiffCalcMode.PrevOffer)
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

			foreach (var offer in Offers.Value) {
				var line = lines.FirstOrDefault(l => l.OfferId == offer.Id);
				if (line != null) {
					offer.AttachOrderLine(line);
				}
			}
		}

		protected abstract void Query();

		public override void Update()
		{
			Query();
			CurrentOffer = CurrentOffer ?? Offers.Value.FirstOrDefault(o => o.Id == initOfferId);
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
			HistoryOrders = (List<SentOrderLine>)cache[HistoryOrdersCacheKey()];
		}

		public void LoadHistoryOrders()
		{
			//таймер может сработать уже после того как мы ушли с формы
			//в этом случае не надо ничего делать
			if (!IsActive)
				return;

			if (CurrentOffer == null || Address == null)
				return;

			var key = HistoryOrdersCacheKey();
			var cached = (List<SentOrderLine>)cache[key];
			if (cached != null) {
				HistoryOrders = cached;
			}
			else {
				IQueryable<SentOrderLine> query = StatelessSession.Query<SentOrderLine>()
					.OrderByDescending(o => o.Order.SentOn);
				//ошибка в nhibernate, если .Where(o => o.Order.Address == Address)
				//переместить в общий блок то первый
				//where применяться не будет
				var addressId = Address.Id;
				if (Settings.Value.GroupByProduct) {
					var catalogId = CurrentOffer.CatalogId;
					query = query.Where(o => o.CatalogId == catalogId)
						.Where(o => o.Order.Address.Id == addressId);
				}
				else {
					var productId = CurrentOffer.ProductId;
					query = query.Where(o => o.ProductId == productId)
						.Where(o => o.Order.Address.Id == addressId);
				}
				cached = query
					.Fetch(l => l.Order)
					.ThenFetch(o => o.Price)
					.Take(20)
					.ToList();
				cache.Put(key, cached);
			}
			HistoryOrders = cached;

			LoadStat();
		}

		private object HistoryOrdersCacheKey()
		{
			if (CurrentOffer == null || Address == null)
				return 0;

			var currentCacheKey = CurrentOffer.CatalogId;
			if (!Settings.Value.GroupByProduct)
				currentCacheKey = CurrentOffer.ProductId;
			return currentCacheKey;
		}
	}
}
