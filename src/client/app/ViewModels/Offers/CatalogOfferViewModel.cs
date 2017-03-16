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
using System.Windows.Controls;
using System.Collections.ObjectModel;
using Caliburn.Micro;

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

			RetailCost = CurrentOffer.CombineLatest(RetailMarkup, Rounding,
				(o, m, r) => Round(NullableHelper.Round(o?.ResultCost * (1 + m / 100),2), r))
				.ToValue();

			CurrentOffer.Subscribe(_ => RetailMarkup.Recalculate());

			Persist(HideJunk, "HideJunk");
			Persist(GroupByProduct, "GroupByProduct");
			SessionValue(CurrentRegion, "CurrentRegion");
			SessionValue(CurrentFilter, "CurrentFilter");

			PrintMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
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
		NotifyValue<object> currentDisplayItem;
		public NotifyValue<object> CurrentDisplayItem
		{ get
			{
				return currentDisplayItem; }

			set
			{
				currentDisplayItem =value;
			}
		}



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
				Filter(false);
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

		public void UpdateFilters()
		{
			Regions.Value = new[] { Consts.AllRegionLabel }
				.Concat(CatalogOffers.Select(o => o.Price.RegionName).Distinct()
					.OrderBy(r => r))
				.ToList();

			FillProducerFilter(CatalogOffers);
		}

		/// <summary>
		/// Фильтр
		/// </summary>
		/// <param name="fromControl">true - если фильтр вызван пользователем из компонента.
		/// false - если фильтр вызван вручную</param>
		public void Filter(bool fromControl = true)
		{
			if (skipFilter) return;

			var region = CurrentRegion.Value;
			IEnumerable<Offer> offers = CatalogOffers;
			if (region != Consts.AllRegionLabel) {
				offers = offers.Where(o => o.Price.RegionName == region);
			}

			if (fromControl) ProducerFilterStateSet(); else ProducerFilterStateGet(offers.ToList());

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

		/// <summary>
		/// Округление денежного значения.
		/// </summary>
		/// <param name="value">Исходное денежное значение.</param>
		/// <param name="roundingValue">Тип округления.</param>
		/// <returns>Округленное денежное значение.</returns>
		private decimal? Round(decimal? value, Models.Rounding? roundingValue)
		{
			var rounding = roundingValue ?? Models.Rounding.None;
			if (rounding != Models.Rounding.None) {
				var @base = 10;
				var factor = 1;
				if (rounding == Models.Rounding.To1_00) {
					@base = 1;
				}
				else if (rounding == Models.Rounding.To0_50) {
					@factor = 5;
				}
				var normalized = (int?) (value * @base);
				return (normalized - normalized % factor) / (decimal) @base;
			}
			return value;
		}

		public void SetMenuItems()
		{
			var item = new MenuItem { Header = DisplayName };
			PrintMenuItems.Add(item);
		}

		public ObservableCollection<MenuItem> PrintMenuItems { get; set; }
		public string LastOperation { get; set; }
		public string PrinterName { get; set; }
		public bool IsView { get; set; }
		public bool CanPrint => User.CanPrint<CatalogOfferDocument>();

		public PrintResult Print()
		{
			var docs = new List<BaseDocument>();
			if (!IsView) {
				var printItems = PrintMenuItems.Where(i => i.IsChecked).ToList();
				if (!printItems.Any())
					printItems.Add(PrintMenuItems.First());
				foreach (var item in printItems) {
					if ((string)item.Header == DisplayName)
						docs.Add(new CatalogOfferDocument(ViewHeader, GetPrintableOffers()));
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if (String.IsNullOrEmpty(LastOperation) || LastOperation == DisplayName)
				Coroutine.BeginExecute(PrintPreview().GetEnumerator());
			return null;
		}

		public IEnumerable<IResult> PrintPreview()
		{
			return Preview(DisplayName, new CatalogOfferDocument(ViewHeader, GetPrintableOffers()));
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


		/// <summary>
		/// Задает или возвращает тип округления стоимости.
		/// </summary>
		public NotifyValue<Rounding?> Rounding { get; set; }
	}
}