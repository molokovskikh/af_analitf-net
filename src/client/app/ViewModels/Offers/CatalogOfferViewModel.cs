using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Views.Offers;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;
using System.Windows;
using AnalitF.Net.Client.ViewModels.Parts;

namespace AnalitF.Net.Client.ViewModels.Offers
{
	public class CatalogOfferViewModel : BaseOfferViewModel, IPrintable
	{
		private CatalogName filterCatalogName;
		private Catalog filterCatalog;

		public List<Offer> CatalogOffers = new List<Offer>();

		private CatalogOfferViewModel(OfferComposedId initOfferId = null)
			: base(initOfferId)
		{
			InitFields();
			NeedToCalculateDiff = true;
			DisplayName = "Сводный прайс-лист";
			Filters = new[] { "Все", "Основные", "Неосновные" };

			CurrentFilter.Value = Filters[0];
			CurrentRegion.Value = Consts.AllRegionLabel;

			GroupByProduct.Value = Settings.Value.GroupByProduct;
			GroupByProduct.Subscribe(_ => Offers.Value = Sort(Offers.Value));

			RetailMarkup = new NotifyValue<decimal>(true,
				() => MarkupConfig.Calculate(Settings.Value.Markups, CurrentOffer.Value, User, Address),
				Settings);

			RetailCost = CurrentOffer.CombineLatest(RetailMarkup,
				(o, m) => NullableHelper.Round(o?.ResultCost * (1 + m / 100), 2))
				.ToValue();

			CurrentOffer.Subscribe(_ => RetailMarkup.Recalculate());
			Persist(HideJunk, "HideJunk");
			Persist(GroupByProduct, "GroupByProduct");
			SessionValue(CurrentRegion, "CurrentRegion");
			SessionValue(CurrentFilter, "CurrentFilter");
		}

		//для восстановления состояния
		public CatalogOfferViewModel(long id)
			: this()
		{
			filterCatalog = Session.Load<Catalog>((uint)id);
			ViewHeader = filterCatalog.FullName;
			//тк мы фильтруем по каталожному продукту то нет нужды загружать его
			CurrentCatalog.Value = filterCatalog;
		}

		public CatalogOfferViewModel(Catalog catalog, OfferComposedId initOfferId = null)
			: this(initOfferId)
		{
			filterCatalog = catalog;
			ViewHeader = catalog.FullName;
			//тк мы фильтруем по каталожному продукту то нет нужды загружать его
			CurrentCatalog.Value = catalog;
		}

		public CatalogOfferViewModel(CatalogName catalogName)
			: this()
		{
			filterCatalogName = catalogName;
			ViewHeader = filterCatalogName.Name;
		}

		public string ViewHeader { get; }

		public bool IsFilterByCatalogName => filterCatalogName != null;

		public string[] Filters { get; set; }

		public NotifyValue<bool> HideJunk { get; set; }
		public NotifyValue<string> CurrentFilter { get; set; }
		public NotifyValue<List<MaxProducerCost>> MaxProducerCosts { get; set; }
		public NotifyValue<List<string>> Regions { get; set; }
		public NotifyValue<string> CurrentRegion { get; set; }
		public NotifyValue<bool> GroupByProduct { get; set; }
		public NotifyValue<decimal?> RetailCost { get; set; }
		public NotifyValue<decimal> RetailMarkup { get; set; }
		public NotifyValue<List<object>> DisplayItems { get; set; }
		public NotifyValue<object> CurrentDisplayItem { get; set; }

		public bool CanPrint => User.CanPrint<CatalogOfferDocument>();

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Bus.Listen<string>("db")
				.Where(m => m == "Reload")
				.Subscribe(_ => CatalogOffers?.Clear(), CloseCancellation.Token);

			Offers.Select(v => {
					v = v ?? new List<Offer>();
					if (IsFilterByCatalogName) {
						return v.GroupBy(g => g.GroupName)
							.Select(g => new object[] { new GroupHeader(g.Key) }.Concat(g))
							.SelectMany(o => o)
							.ToList();
					}
					else {
						return v.Cast<object>().ToList();
					}
				})
				.Subscribe(DisplayItems);
			CurrentDisplayItem.OfType<Offer>().Subscribe(CurrentOffer);
			CurrentOffer.Subscribe(CurrentDisplayItem);
			//.Skip(1) - пропускаем начальные значения
			CurrentRegion.Cast<Object>().Skip(1)
				.Merge(CurrentFilter.Skip(1))
				.Merge(CurrentProducer.Skip(1))
				.Merge(HideJunk.Select(v => (object)v).Skip(1))
				.Subscribe(_ => Filter());
			DbReloadToken.SelectMany(_ => Env.RxQuery(s => {
				if (IsFilterByCatalogName) {
					var catalogs = s.Query<Catalog>()
						.Fetch(c => c.Name)
						.Where(c => c.Name == filterCatalogName).ToArray();
					var ids = catalogs.Select(c => c.Id).ToArray();

					var result = s.Query<Offer>()
						.Where(o => ids.Contains(o.CatalogId))
						.Fetch(o => o.Price)
						.ToList();
					result.Each(o => o.GroupName = catalogs.Where(c => c.Id == o.CatalogId)
						.Select(c => c.FullName)
						.FirstOrDefault());
					return result;
				} else {
					var catalogId = filterCatalog.Id;
					return s.Query<Offer>().Where(o => o.CatalogId == catalogId)
						.Fetch(o => o.Price)
						.ToList();
				}
			})).CatchSubscribe(x => {
				CatalogOffers = x;
				UpdateFilters();
				Filter();
				UpdateOffers(Offers.Value);
				if (x.Count == 0) {
					Manager.Warning("Нет предложений");
					TryClose();
				}
			}, CloseCancellation);

			UpdateMaxProducers();
		}

		protected override void SelectOffer()
		{
			CurrentOffer.Value = CurrentOffer.Value
				?? Offers.Value.FirstOrDefault(o => o.Id == initOfferId)
				?? Offers.Value.FirstOrDefault(o => o.Price.BasePrice)
				?? Offers.Value.FirstOrDefault();
		}

		private void UpdateMaxProducers()
		{
			if (CurrentCatalog.Value == null) {
				MaxProducerCosts.Value = new List<MaxProducerCost>();
				return;
			}
			var catalogId = CurrentCatalog.Value.Id;
			Env.RxQuery(s => s.Query<MaxProducerCost>()
					.Where(c => c.CatalogId == catalogId)
					.OrderBy(c => c.Product)
					.ThenBy(c => c.Producer)
					.ToList())
				.Subscribe(MaxProducerCosts, CloseCancellation.Token);
		}

		private void UpdateFilters()
		{
			Regions.Value = new[] { Consts.AllRegionLabel }
				.Concat(CatalogOffers.Select(o => o.Price.RegionName).Distinct()
					.OrderBy(r => r))
				.ToList();

			FillProducerFilter(CatalogOffers);
		}

		public void Filter()
		{
			var region = CurrentRegion.Value;
			IEnumerable<Offer> offers = CatalogOffers;
			if (region != Consts.AllRegionLabel) {
				offers = offers.Where(o => o.Price.RegionName == region);
			}

			ProducerFilterStateGet(offers.ToList());

			var producer = CurrentProducer.Value;
			if (producer != null && producer.Id > 0) {
				var id = producer.Id;
				offers = offers.Where(o => o.ProducerId == id);
			}
			var filter = CurrentFilter.Value;
			if (filter == Filters[1]) {
				offers = offers.Where(o => o.Price.BasePrice);
			}
			if (filter == Filters[2]) {
				offers = offers.Where(o => !o.Price.BasePrice);
			}
			if (HideJunk) {
				offers = offers.Where(o => !o.Junk);
			}

			Offers.Value = Sort(offers.ToList());
		}

		private List<Offer> Sort(IList<Offer> offers)
		{
			if (offers == null)
				return null;

			if (GroupByProduct) {
				return SortByMinCostInGroup(offers, o => o.ProductId);
			}
			else {
				return SortByMinCostInGroup(offers, o => o.CatalogId, false);
			}
		}

		public PrintResult Print()
		{
			var doc = new CatalogOfferDocument(ViewHeader, GetPrintableOffers());
			return new PrintResult(DisplayName, doc);
		}

		public void ShowPrice()
		{
			if (CurrentOffer.Value == null)
				return;

			if (Shell.Navigator is Navigator) {
				var price = CurrentOffer.Value.Price;
				var prices = new PriceViewModel {
					CurrentPrice = {
						Value = price
					}
				};
				var offerViewModel = new PriceOfferViewModel(price.Id, prices.ShowLeaders, CurrentOffer.Value.Id);
				((Navigator)Shell.Navigator).NavigateAndReset(prices, offerViewModel);
			} else {
				var price = CurrentOffer.Value.Price;
				var offers = new PriceOfferViewModel(price.Id, false, CurrentOffer.Value.Id);
				Shell.Navigate(offers);
			}
		}

		public void SearchInCatalog(object sender, TextCompositionEventArgs args)
		{
			var text = args.Text;
			if (Shell == null)
				return;
			CatalogViewModel catalog;
			if (Shell.Navigator is TabNavigator)
				catalog = Shell.NavigationStack.LastOrDefault() as CatalogViewModel;
			else
				catalog = Shell.NavigationStack.Reverse().Skip(1).FirstOrDefault() as CatalogViewModel;
			if (catalog == null)
				return;
			if (text.All(char.IsControl))
				return;

			args.Handled = true;
			catalog.SearchText = text;
			TryClose();
		}

		public override void TryClose()
		{
			ProducerFilterStateSet();
			base.TryClose();
		}

		public void Delete()
		{
			if (Manager.Question("Удалить значение?") != MessageBoxResult.Yes)
				return;
			CurrentOffer.Value.OrderCount = null;
			CurrentOffer.Value.UpdateOrderLine(ActualAddress, Settings.Value, Confirm, AutoCommentText);
		}

#if DEBUG
		public override object[] GetRebuildArgs()
		{
			return new object[] {
				filterCatalog.Id
			};
		}
#endif
	}
}