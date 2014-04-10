using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using Devart.Data.MySql;
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
		public virtual bool DoNotHaveOffers
		{
			get { return !HaveOffers; }
		}

		public CatalogDisplayItem(uint id, string name, string form, bool haveOffers, bool vitallyImportant)
		{
			CatalogId = id;
			Name = name;
			Form = form;
			HaveOffers = haveOffers;
			VitallyImportant = vitallyImportant;
		}

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
			Readonly = true;

			Shell = catalog.Shell;
			Items = new NotifyValue<List<CatalogDisplayItem>>();
			CurrentItem = new NotifyValue<CatalogDisplayItem>();
			var changes = CurrentItem.Changed()
#if !DEBUG
				.Throttle(Consts.ScrollLoadTimeout)
				.ObserveOn(UiScheduler)
#endif
				;
			CurrentCatalog = changes.ToValue(_ => {
				if (CurrentItem.Value == null)
					return null;
				var catalogId = CurrentItem.Value.CatalogId;
				return StatelessSession.Query<Catalog>()
					.Fetch(c => c.Name)
					.ThenFetch(n => n.Mnn)
					.FirstOrDefault(c => c.Id == catalogId);
			});
			ParentModel = catalog;
			QuickSearch = new QuickSearch<CatalogDisplayItem>(UiScheduler,
				v => Items.Value.FirstOrDefault(c => c.Name.StartsWith(v, StringComparison.CurrentCultureIgnoreCase)),
				v => CurrentItem.Value = v);
			QuickSearch.IsEnabled = false;

			//после закрытия формы нужно отписаться от событий родительской формы
			//что бы не делать лишних обновлений
			OnCloseDisposable.Add(ParentModel.ObservableForProperty(m => (object)m.FilterByMnn)
				.Merge(ParentModel.ObservableForProperty(m => (object)m.CurrentFilter))
				.Merge(ParentModel.ObservableForProperty(m => (object)m.ShowWithoutOffers))
				.Subscribe(_ => Update()));

			SearchBehavior = new SearchBehavior(this);
		}

		public CatalogViewModel ParentModel { get; set; }
		public SearchBehavior SearchBehavior { get; set; }
		public QuickSearch<CatalogDisplayItem> QuickSearch { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Shell = ParentModel.Shell;
		}

		public IResult Search()
		{
			return SearchBehavior.Search();
		}

		public IResult ClearSearch()
		{
			return SearchBehavior.ClearSearch();
		}

		public override void Update()
		{
			//мы не можем использовать nhibernate для выборки данных тк объем данных слишком велик
			//выборка может достигать 10**5 записей
			var connection = StatelessSession.Connection;
			var command = connection.CreateCommand();
			var conditions = new List<string>();

			if (ParentModel.FiltredMnn != null) {
				conditions.Add("cn.MnnId = @mnnId");
				var parameter = command.CreateParameter();
				parameter.ParameterName = "@mnnId";
				parameter.Value = ParentModel.FiltredMnn.Id;
				command.Parameters.Add(parameter);
			}

			if (!string.IsNullOrEmpty(SearchBehavior.ActiveSearchTerm.Value)) {
				conditions.Add("(cn.Name like @term or c.Form like @term)");
				var parameter = command.CreateParameter();
				parameter.ParameterName = "@term";
				parameter.Value = SearchBehavior.ActiveSearchTerm.Value;
				parameter.Direction = ParameterDirection.Input;
				parameter.Value = "%" + SearchBehavior.ActiveSearchTerm.Value + "%";
				command.Parameters.Add(parameter);
			}

			if (!ParentModel.ShowWithoutOffers) {
				conditions.Add("c.HaveOffers = 1");
			}

			if (ParentModel.CurrentFilter == ParentModel.Filters[1]) {
				conditions.Add("c.VitallyImportant = 1");
			}

			if (ParentModel.CurrentFilter == ParentModel.Filters[2]) {
				conditions.Add("c.MandatoryList = 1");
			}

			command.CommandText = "select c.Id, cn.Name, c.Form, c.HaveOffers, c.VitallyImportant"
				+ " from Catalogs c"
				+ " join CatalogNames cn on cn.Id = c.NameId"
				+ (conditions.Count > 0 ? " where " + conditions.Implode(" and ") : "")
				+ " order by cn.Name, c.Form";
			var items = new List<CatalogDisplayItem>();
			using (var reader = command.ExecuteReader()) {
				while (reader.Read()) {
					items.Add(new CatalogDisplayItem(
						(uint)reader.GetInt32(0),
						reader.GetString(1),
						reader.GetString(2),
						reader.GetBoolean(3),
						reader.GetBoolean(4)));
				}
			}
			Items.Value = items;
			if (CurrentItem.Value == null)
				CurrentItem.Value = items.FirstOrDefault();
		}

		public NotifyValue<List<CatalogDisplayItem>> Items { get; set;}
		public NotifyValue<CatalogDisplayItem> CurrentItem { get; set; }
		public NotifyValue<Catalog> CurrentCatalog { get; set; }

		public IResult EnterItem()
		{
			if (CurrentCatalog.Value == null)
				return null;

			if (!CurrentCatalog.Value.HaveOffers)
				return new ShowPopupResult();

			Shell.Navigate(new CatalogOfferViewModel(CurrentCatalog));
			return null;
		}
	}
}