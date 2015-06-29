using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.NHibernate;
using Common.Tools;
using Dapper;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using ReactiveUI;
using Message = Common.Tools.Message;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public abstract class BaseOfferViewModel : BaseScreen
	{
		private string autoCommentText;
		private bool resetAutoComment;
		private Catalog currentCatalog;
		private List<SentOrderLine> historyOrders;

		protected Producer EmptyProducer = new Producer(Consts.AllProducerLabel);
		//тк уведомление о сохранении изменений приходит после
		//изменения текущего предложения
		protected NotifyValue<Offer> LastEditOffer;
		protected bool NeedToCalculateDiff;
		protected bool NavigateOnShowCatalog;
		//адрес доставки для текущего элемента, нужен если мы отображаем элементы которые относятся к разным адресам доставки
		protected Address CurrentElementAddress;
		protected OfferComposedId initOfferId;

		public BaseOfferViewModel(OfferComposedId initOfferId = null)
		{
			Readonly = true;
			UpdateOnActivate = false;

			this.initOfferId = initOfferId;

			LastEditOffer = new NotifyValue<Offer>();
			Offers = new NotifyValue<IList<Offer>>(new List<Offer>());
			CurrentOffer = new NotifyValue<Offer>();
			CurrentProducer = new NotifyValue<Producer>(EmptyProducer);
			Producers = new NotifyValue<List<Producer>>(new List<Producer> { EmptyProducer });

			CurrentOffer.Subscribe(_ => {
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

			Settings.Subscribe(_ => Calculate());
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

		public NotifyValue<List<Producer>> Producers { get; set; }

		public NotifyValue<Producer> CurrentProducer { get; set; }

		[Export]
		public NotifyValue<IList<Offer>> Offers { get; set; }

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

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (StatelessSession != null) {
				Promotions = new PromotionPopup(StatelessSession, Shell.Config);
				this.ObservableForProperty(m => m.CurrentCatalog, skipInitial: false)
					.Subscribe(c => Promotions.Activate(c.Value == null ? null : c.Value.Name));
			}

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			CurrentOffer
				.Where(x => x != null)
				.Throttle(Consts.LoadOrderHistoryTimeout, Scheduler)
				.ObserveOn(UiScheduler)
				.Subscribe(_ => LoadHistoryOrders(), CloseCancellation.Token);

			CurrentOffer
#if !DEBUG
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
#endif
				.Subscribe(_ => {
					if (Session != null) {
						if (CurrentOffer.Value != null && (currentCatalog == null || CurrentCatalog.Id != CurrentOffer.Value.CatalogId)) {
							var sql = @"select c.*, cn.*, m.*, cn.DescriptionId as Id
from Catalogs c
join CatalogNames cn on cn.Id = c.NameId
left join Mnns m on m.Id = cn.MnnId
where c.Id = ?";
							CurrentCatalog = Session
								.Connection.Query<Catalog, CatalogName, Mnn, ProductDescription, Catalog>(sql, (c, cn, m, d) => {
									c.Name = cn;
									if (cn != null) {
										cn.Mnn = m;
										cn.Description = d;
									}
									return c;
								}, new { catalogId = CurrentOffer.Value.CatalogId }).FirstOrDefault();
						}
					}
				}, CloseCancellation.Token);

			//изменения в LastEditOffer могут произойти уже после перехода на другое предложение
			//по этому нужно отслеживать изменения в CurrentOffer и LastEditOffer
			var observable = this.ObservableForProperty(m => m.CurrentOffer.Value.OrderCount)
				.Merge(this.ObservableForProperty(m => m.LastEditOffer.Value.OrderCount))
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));

			OnCloseDisposable.Add(Bus.RegisterMessageSource(observable));
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			if (Shell != null) {
				ResetAutoComment = Shell.ResetAutoComment;
				AutoCommentText = Shell.AutoCommentText;
			}
		}

		protected override void OnDeactivate(bool close)
		{
			if (Shell != null) {
				Shell.AutoCommentText = AutoCommentText;
				Shell.ResetAutoComment = ResetAutoComment;
			}

			if (Session != null) {
				var newLines = Addresses.Where(a => NHibernateUtil.IsInitialized(a.Orders))
					.SelectMany(a => a.Orders)
					.Where(o => NHibernateUtil.IsInitialized(o.Lines))
					.SelectMany(o => o.Lines)
					.Where(l => l.Id == 0)
					.ToList();
				if (newLines.Count > 0) {
					var condition = newLines.Implode(l => String.Format("(a.CatalogId = {0} and (a.ProducerId = {1} or a.Producerid is null))",
						l.CatalogId, l.ProducerId != null ? l.ProducerId.ToString() : "null"), " or ");
					Session.CreateSQLQuery(String.Format("delete a from AwaitedItems a where {0}", condition))
						.ExecuteUpdate();
				}
			}

			base.OnDeactivate(close);
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

		public static List<Offer> SortByMinCostInGroup<T>(IList<Offer> offer, Func<Offer, T> key, bool setGroupKey = true)
		{
			var lookup = offer.GroupBy(key)
				.ToDictionary(g => g.Key, g => g.Min(o => o.ResultCost));

			var offers = offer.OrderBy(o => Tuple.Create(lookup[key(o)], key(o), o.ResultCost)).ToList();

			var indexes = lookup.OrderBy(k => Tuple.Create(k.Value, k.Key))
				.Select((k, i) => Tuple.Create(k.Key, i))
				.ToDictionary(t => t.Item1, t => t.Item2);

			offers.Each(o => {
				o.IsGrouped = setGroupKey && indexes[key(o)] % 2 > 0;
			});

			return offers;
		}

		public virtual void OfferUpdated()
		{
			var offer = CurrentOffer.Value;
			if (offer == null)
				return;

			if (offer.Price.IsOrderDisabled) {
				offer.OrderCount = null;
				return;
			}
			LastEditOffer.Value = offer;
			LoadStat();
			ShowValidationError(offer.UpdateOrderLine(ActualAddress, Settings.Value, Confirm, AutoCommentText));
		}

		public virtual void OfferCommitted()
		{
			var offer = LastEditOffer.Value;
			if (offer == null)
				return;

			ShowValidationError(offer.SaveOrderLine(ActualAddress, Settings.Value, Confirm, AutoCommentText));
		}

		protected void ShowValidationError(List<Message> messages)
		{
			OrderWarning.Show(messages);

			//если человек ушел с этой позиции а мы откатываем значение то нужно вернуть его к этой позиции что бы он
			//мог ввести корректное значение
			var errors = messages.Where(m => m.IsError);
			if (errors.Any()) {
				if (LastEditOffer.Value != null
					&& (CurrentOffer.Value == null || CurrentOffer.Value.Id != LastEditOffer.Value.Id)) {
					CurrentOffer.Value = LastEditOffer.Value;
				}
			}
		}

		protected void Calculate()
		{
			Calculate(Offers.Value ?? new List<Offer>());
		}

		protected void Calculate(IEnumerable<Offer> offers)
		{
			if (Address == null)
				return;

			if (NeedToCalculateDiff)
				CalculateDiff(offers);

			CalculateRetailCost(offers);

			if (Settings.Value.WarnIfOrderedYesterday && Address.YesterdayOrderedProductIds == null) {
				var addressId = Address.Id;
				RxQuery(s => s.CreateSQLQuery(@"select ProductId
from SentOrderLines l
join SentOrders o on o.Id = l.OrderId
where o.SentOn > :begin and o.SentOn < :end and o.AddressId = :addressId
group by l.ProductId")
					.SetParameter("begin", DateTime.Today.AddDays(-1))
					.SetParameter("end", DateTime.Today)
					.SetParameter("addressId", addressId)
					.List<object>()
					.Select(Convert.ToUInt32)
					.ToList())
					.ObserveOn(UiScheduler)
					.Subscribe(x => Address.YesterdayOrderedProductIds = x, CloseCancellation.Token);
			}
		}

		private void CalculateRetailCost(IEnumerable<Offer> offers)
		{
			offers.Each(o => o.CalculateRetailCost(Settings.Value.Markups, User));
		}

		private void CalculateDiff(IEnumerable<Offer> offers)
		{
			decimal baseCost = 0;
			var diffCalcMode = Settings.Value.DiffCalcMode;
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

		protected virtual void LoadOrderItems()
		{
			LoadOrderItems(Offers.Value);
		}

		protected void LoadOrderItems(IEnumerable<Offer> offers)
		{
			foreach (var offer in offers) {
				offer.Price.Order = null;
				offer.OrderLine = null;
			}

			if (ActualAddress == null)
				return;

			var activeOrders = ActualAddress.ActiveOrders();
			var orders = activeOrders.ToLookup(o => o.Price.Id);
			var lines = ActualAddress.ActiveOrders().SelectMany(o => o.Lines).ToLookup(l => l.OfferId);
			foreach (var offer in offers) {
				offer.Price.Order = orders[offer.Price.Id].FirstOrDefault();
				offer.OrderLine = lines[offer.Id].FirstOrDefault();
			}
		}

		protected virtual void Query()
		{
		}

		protected override void RecreateSession()
		{
			base.RecreateSession();

			if (CurrentElementAddress != null)
				CurrentElementAddress = Session.Get<Address>(CurrentElementAddress.Id);
			LoadOrderItems();
		}

		public override void Update()
		{
			Query();
			SelectOffer();
			Calculate();
			LoadOrderItems();
		}

		protected virtual void SelectOffer()
		{
			CurrentOffer.Value = CurrentOffer.Value
				?? Offers.Value.FirstOrDefault(o => o.Id == initOfferId)
				?? Offers.Value.FirstOrDefault();
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
			HistoryOrders = (List<SentOrderLine>)Cache[HistoryOrdersCacheKey()];
		}

		public void LoadHistoryOrders()
		{
			if (ActualAddress == null || CurrentOffer.Value == null || StatelessSession == null)
				return;
			var key = HistoryOrdersCacheKey();
			HistoryOrders = Util.Cache(Cache, key, k => {
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

		private object HistoryOrdersCacheKey()
		{
			if (CurrentOffer.Value == null || ActualAddress == null)
				return 0;

			if (Settings.Value.GroupByProduct)
				return CurrentOffer.Value.ProductId;
			else
				return CurrentOffer.Value.CatalogId;
		}

		public void FillProducerFilter(IEnumerable<Offer> offers)
		{
			Producers.Value = new[] { EmptyProducer }
				.Concat(offers.Where(o => o.ProducerId != null)
					.GroupBy(p => p.ProducerId)
					.Select(g => new Producer(g.Key.Value, g.First().Producer))
					.OrderBy(p => p.Name))
				.ToList();
		}

		public override void TryClose()
		{
			OfferCommitted();
			base.TryClose();
		}

		protected IList<Offer> GetPrintableOffers()
		{
			return GetItemsFromView<Offer>("Offers") ?? Offers.Value;
		}
	}
}
