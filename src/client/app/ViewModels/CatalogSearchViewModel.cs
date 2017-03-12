using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using Dapper;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class CatalogDisplayItem
	{
		public uint CatalogId { get; set; }
		public string Name { get; set; }
		public string Form { get; set; }
		public bool HaveOffers { get; set; }
		[Style(Description = "Жизненно важный")]
		public bool VitallyImportant { get; set; }

		[Style(Description = "Предложения отсутствуют")]
		public virtual bool DoNotHaveOffers => !HaveOffers;

		//перегрузка Equals и GetHashCode
		//нужна что бы DataGrid сохранял выделенную позицию после обновления данных
		public override bool Equals(object obj)
		{
			var that = obj as CatalogDisplayItem;
			if (that == null)
				return false;

			if (CatalogId == 0 && that.CatalogId == 0)
				return base.Equals(obj);

			return CatalogId == that.CatalogId;
		}

		public override int GetHashCode()
		{
			if (CatalogId == 0)
				return base.GetHashCode();
			return CatalogId.GetHashCode();
		}
	}

	public class CatalogSearchViewModel : BaseScreen
	{
		public CatalogSearchViewModel(CatalogViewModel catalog)
		{
			Shell = catalog.Shell;
			InitFields();
			Items.Value = new List<CatalogDisplayItem>();
			CurrentItem
#if !DEBUG
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
#endif
				.SelectMany(x => Env.RxQuery(s => {
					if (x == null)
						return null;
					var catalogId = x.CatalogId;
					return s.Query<Catalog>()
						.Fetch(c => c.Name)
						.ThenFetch(n => n.Mnn)
						.FirstOrDefault(c => c.Id == catalogId);
				}))
				.Subscribe(CurrentCatalog, CloseCancellation.Token);
			ParentModel = catalog;
			QuickSearch = new QuickSearch<CatalogDisplayItem>(UiScheduler,
				v => Items.Value.FirstOrDefault(c => c.Name.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				CurrentItem);
			QuickSearch.IsEnabled = false;

			SearchBehavior = new SearchBehavior(this);
			IsLoading.Value = true;
			IsQuickSearchEnabled.Subscribe(v => {
				QuickSearch.IsEnabled = v;
				SearchBehavior.HandleGridKeyboardInput = !v;
			});
		}

		public NotifyValue<bool> IsQuickSearchEnabled { get; set; }
		public CatalogViewModel ParentModel { get; set; }
		public SearchBehavior SearchBehavior { get; set; }
		public QuickSearch<CatalogDisplayItem> QuickSearch { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }
		[Export]
		public NotifyValue<List<CatalogDisplayItem>> Items { get; set;}
		public NotifyValue<CatalogDisplayItem> CurrentItem { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Shell = ParentModel.Shell;

			//если установили фильтр по мнн нужно сбросить поисковый запрос что бы отобразить все
			//товары с таким мнн
			ParentModel.ObservableForProperty(m => m.FilterByMnn).Where(x => x.Value)
				.Subscribe(_ => SearchBehavior.ActiveSearchTerm.Value = "");

			//после закрытия формы нужно отписаться от событий родительской формы
			//что бы не делать лишних обновлений
			ParentModel.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Merge(SearchBehavior.ActiveSearchTerm.Cast<Object>())
				.Throttle(TimeSpan.FromMilliseconds(30), Scheduler)
				.Do(_ => IsLoading.Value = true)
				.Select(_ => RxQuery(LoadData))
				.Switch()
				.Do(_ => IsLoading.Value = false)
				.Subscribe(Items, CloseCancellation.Token);

			Items.Subscribe(_ => {
				CurrentItem.Value = (Items.Value ?? Enumerable.Empty<CatalogDisplayItem>()).FirstOrDefault();
			});
		}

		public List<CatalogDisplayItem> LoadData(IStatelessSession session)
		{
			//мы не можем использовать nhibernate для выборки данных тк объем данных слишком велик
			//выборка может достигать 10**5 записей
			var connection = session.Connection;
			var conditions = new List<string>();

			if ((ParentModel.CurrentFiltercategory != null) && (ParentModel.CurrentFiltercategory.Id > 0)){
				conditions.Add("c.CategoryId = " + ParentModel.CurrentFiltercategory.Id.ToString());
			}                
			if (ParentModel.FiltredMnn != null) {
				conditions.Add("cn.MnnId = @mnnId");
			}
			if (!string.IsNullOrEmpty(SearchBehavior.ActiveSearchTerm.Value)) {
				conditions.Add("(cn.Name like @term or c.Form like @term)");
			}
			if (!ParentModel.ShowWithoutOffers) {
				if (ParentModel.Mode == CatalogViewMode.Basic)
					conditions.Add("c.HaveOffers = 1");
				else if (ParentModel.Mode == CatalogViewMode.CatalogSelector)
					conditions.Add("exists ( select * from Waybilllines l join Waybills w on w.Id = l.WaybillId where w.DocType = 1 and w.Status = 1 and l.CatalogId = c.Id )");
			}
			var filterType = ParentModel.CurrentFilter?.FilterType;
			if (ParentModel.CurrentFilter == ParentModel.Filters[1]) {
				conditions.Add("c.VitallyImportant = 1");
			}
			if (ParentModel.CurrentFilter == ParentModel.Filters[2]) {
				conditions.Add("c.MandatoryList = 1");
			}
			if (ParentModel.CurrentFilter == ParentModel.Filters[3]) {
				conditions.Add("exists ( select * from AwaitedItems a where a.CatalogId = c.Id )");
			}
			if (filterType == FilterType.PKU) {
				conditions.Add("(c.Narcotic = 1 || c.Toxic = 1 || c.Combined = 1 || c.Other = 1)");
			}
			if (filterType == FilterType.PKUNarcotic) {
				conditions.Add("c.Narcotic = 1");
			}
			if (filterType == FilterType.PKUToxic) {
				conditions.Add("c.Toxic = 1");
			}
			if (filterType == FilterType.PKUCombined) {
				conditions.Add("c.Combined = 1");
			}
			if (filterType == FilterType.PKUOther) {
				conditions.Add("c.Other = 1");
			}

			var sql = "select c.Id as CatalogId, cn.Name, c.Form, c.HaveOffers, c.VitallyImportant"
				+ " from Catalogs c"
				+ " join CatalogNames cn on cn.Id = c.NameId"
				+ (conditions.Count > 0 ? " where " + conditions.Implode(" and ") : "")
				+ " order by cn.Name, c.Form";
			return connection.Query<CatalogDisplayItem>(sql, new {
				mnnId = ParentModel.FiltredMnn?.Id,
				term = "%" + SearchBehavior.ActiveSearchTerm.Value + "%"
			}).ToList();
		}


		public IResult EnterItem()
		{
			if (CurrentItem.Value == null)
				return null;

			if (!CurrentItem.Value.HaveOffers)
				return new ShowPopupResult(() => ParentModel.ShowOrderHistory());

			Shell.Navigate(new CatalogOfferViewModel(CurrentItem.Value.CatalogId));
			return null;
		}
	}
}