using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Orders;
using AnalitF.Net.Client.Views.Parts;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class FilterDeclaration
	{
		public FilterDeclaration(string name)
		{
			Name = name;
		}

		public FilterDeclaration(string name, string filterDescription, string filterDescriptionWithMnn)
		{
			Name = name;
			FilterDescription = filterDescription;
			FilterDescriptionWithMnn = filterDescriptionWithMnn;
		}

		public string Name { get; set; }
		public string FilterDescription { get; set; }
		public string FilterDescriptionWithMnn { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}

	[DataContract]
	public class CatalogViewModel : BaseScreen
	{
		private bool showWithoutOffers;
		private FilterDeclaration currentFilter;
		private Mnn filtredMnn;
		private bool viewOffersByCatalog;
		private BaseScreen activeItem;
		private IDisposable observable = Disposable.Empty;

		public CatalogViewModel()
		{
			ViewOffersByCatalog = true;
			ViewOffersByCatalogEnabled = Settings.Select(s => s.CanViewOffersByCatalogName).ToValue();

			DisplayName = "Поиск препаратов в каталоге";
			Filters = new[] {
				new FilterDeclaration("Все"),
				new FilterDeclaration("Жизненно важные", "жизненно важным", "только жизненно важные"),
				new FilterDeclaration("Обязательный ассортимент", "обязательному ассортименту", "только обязательные ассортимент"),
				new FilterDeclaration("Ожидаемые позиции", "ожидаемым позициям", "только ожидаемые позиции"),
			};
			CurrentFilter = Filters[0];

			CatalogSearch = new NotifyValue<bool>();
			this.ObservableForProperty(m => m.ActiveItem)
				.Select(i => i.Value != null ? i.Value.CanExport : Observable.Return(false))
				.Switch()
				.Subscribe(CanExport);
			CatalogSearch.CatchSubscribe(_ => UpdateActiveItem(), CloseCancellation);

			CanAddToAwaited = this
				.ObservableForProperty(m => (object)m.CurrentCatalog, skipInitial: false)
				.Merge(this.ObservableForProperty(m => (object)m.CurrentCatalogName))
				.Select(v => GuessCatalog() != null)
				.ToValue();

			this.ObservableForProperty(m => m.CurrentCatalogName)
				.Subscribe(_ => NotifyOfPropertyChange("CanShowDescription"));

			ViewOffersByCatalogVisible = CatalogSearch.Select(v => !v)
				.ToValue();

			OnCloseDisposable.Add(Disposable.Create(() => {
				if (ActiveItem is IDisposable) {
					((IDisposable)ActiveItem).Dispose();
				}
			}));
			IsEnabled = new NotifyValue<bool>(true);
		}

		public NotifyValue<bool> ViewOffersByCatalogVisible { get; private set; }
		public NotifyValue<bool> ViewOffersByCatalogEnabled { get; private set; }
		public NotifyValue<bool> CanAddToAwaited { get; set; }
		public NotifyValue<bool> IsEnabled { get; set; }
		[DataMember]
		public NotifyValue<bool> CatalogSearch { get; set; }

		public override IResult Export()
		{
			if (!CanExport)
				return null;
			return ActiveItem.Export();
		}

		public string SearchText
		{
			get
			{
				if (ActiveItem is CatalogNameViewModel)
					return ((CatalogNameViewModel)ActiveItem).CatalogNamesSearch.SearchText;
				else
					return ((CatalogSearchViewModel)ActiveItem).SearchBehavior.SearchText;
			}
			set
			{
				if (ActiveItem is CatalogNameViewModel)
					((CatalogNameViewModel)ActiveItem).CatalogNamesSearch.SearchText = value;
				else
					((CatalogSearchViewModel)ActiveItem).SearchBehavior.SearchText.Value = value;
			}
		}

		public BaseScreen ActiveItem
		{
			get { return activeItem; }
			set
			{
				if (activeItem != null) {
					//деактивация открытого элемента будет ждать завершения фоновой загрузки данных
					//что заблокирует ui нитку, что бы избежат этого
					//1. отключаем ui - что бы не получать больше запросов
					//2. ждем пока все фоновые запросы не завершатся
					//3. освобождаем старую форму и конструируем новую форму
					//4. включаем ui
					IsEnabled.Value = false;
					activeItem.WaitQueryDrain()
						.ContinueWith(t => {
							ScreenExtensions.TryDeactivate(activeItem, true);
							if (activeItem is IDisposable) {
								((IDisposable)activeItem).Dispose();
							}
							activeItem = value;
							if (IsActive) {
								ScreenExtensions.TryActivate(activeItem);
							}
							NotifyOfPropertyChange("ActiveItem");
							IsEnabled.Value = true;
						}, TplUiScheduler);
				}
				else {
					ScreenExtensions.TryDeactivate(activeItem, true);
					if (activeItem is IDisposable) {
						((IDisposable)activeItem).Dispose();
					}
					activeItem = value;
					if (IsActive) {
						ScreenExtensions.TryActivate(activeItem);
					}
					NotifyOfPropertyChange("ActiveItem");
				}
			}
		}

		[DataMember]
		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				NotifyOfPropertyChange("ShowWithoutOffers");
			}
		}

		public FilterDeclaration[] Filters { get; set; }

		public FilterDeclaration CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				FilterByMnn = false;
				NotifyOfPropertyChange("CurrentFilter");
				NotifyOfPropertyChange("FilterDescription");
			}
		}

		public CatalogName CurrentCatalogName
		{
			get
			{
				if (activeItem is CatalogSearchViewModel) {
					var catalog = ((CatalogSearchViewModel)activeItem).CurrentCatalog.Value;
					return catalog == null ? null : catalog.Name;
				}
				return ((CatalogNameViewModel)activeItem).CurrentCatalogName.Value;
			}
		}

		public Catalog CurrentCatalog
		{
			get
			{
				if (activeItem == null)
					return null;
				if (activeItem is CatalogSearchViewModel) {
					return ((CatalogSearchViewModel)activeItem).CurrentCatalog.Value;
				}
				return ((CatalogNameViewModel)activeItem).CurrentCatalog;
			}
			set
			{
				if (activeItem == null)
					return;
				if (activeItem is CatalogSearchViewModel) {
					((CatalogSearchViewModel)activeItem).CurrentCatalog.Value = value;
				}
				((CatalogNameViewModel)activeItem).CurrentCatalog = value;
			}
		}

		public object CurrentItem
		{
			get
			{
				if (activeItem is CatalogSearchViewModel) {
					return ((CatalogSearchViewModel)activeItem).CurrentCatalog.Value;
				}
				return ((CatalogNameViewModel)activeItem).CurrentItem.Value;
			}
		}

		public bool FilterByMnn
		{
			get { return filtredMnn != null; }
			set
			{
				if (value) {
					if (CurrentCatalogName != null) {
						filtredMnn = CurrentCatalogName.Mnn;
					}
				}
				else {
					filtredMnn = null;
				}
				NotifyOfPropertyChange("FilterByMnn");
				NotifyOfPropertyChange("FiltredMnn");
				NotifyOfPropertyChange("FilterDescription");
			}
		}

		public Mnn FiltredMnn
		{
			get { return filtredMnn; }
			set
			{
				filtredMnn = value;
				if (!filtredMnn.HaveOffers)
					ShowWithoutOffers = true;
				NotifyOfPropertyChange("FiltredMnn");
				NotifyOfPropertyChange("FilterByMnn");
				NotifyOfPropertyChange("FilterDescription");
			}
		}

		public string FilterDescription
		{
			get
			{
				var parts = new List<string>();
				if (FiltredMnn != null)
					parts.Add(String.Format("\"{0}\"", FiltredMnn.Name));

				if (CurrentFilter != null) {
					var filter = FiltredMnn == null ? currentFilter.FilterDescription : currentFilter.FilterDescriptionWithMnn;
					if (!String.IsNullOrEmpty(filter))
						parts.Add(filter);
				}

				if (parts.Count > 0)
					parts.Insert(0, "Фильтр по");

				return parts.Implode(" ");
			}
		}

		public bool CanShowDescription
		{
			get
			{
				return CurrentCatalogName != null && CurrentCatalogName.Description != null;
			}
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;

			Manager.ShowDialog(new DocModel<ProductDescription>(CurrentCatalogName.Description.Id));
		}

		public override void NavigateBackward()
		{
			if (FilterByMnn) {
				FilterByMnn = false;
				return;
			}

			if (!CurrentFilter.Name.Match("Все")) {
				CurrentFilter = Filters[0];
				return;
			}

			base.NavigateBackward();
		}

		public void SwitchMnnFilter()
		{
			FilterByMnn = !FilterByMnn;
		}

		public void SwitchViewOffersByCatalog()
		{
			if (ViewOffersByCatalogEnabled)
				ViewOffersByCatalog = !ViewOffersByCatalog;
		}

		[DataMember]
		public bool ViewOffersByCatalog
		{
			get { return viewOffersByCatalog; }
			set
			{
				viewOffersByCatalog = value;
				NotifyOfPropertyChange("ViewOffersByCatalog");
			}
		}


		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (ActiveItem != null)
				ActiveItem.Shell = Shell;
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			ScreenExtensions.TryActivate(ActiveItem);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
			ScreenExtensions.TryDeactivate(ActiveItem, close);
		}

		public void UpdateActiveItem()
		{
			if (CatalogSearch && !(ActiveItem is CatalogSearchViewModel)) {
				observable.Dispose();
				CleanCache();

				var model = new CatalogSearchViewModel(this);
				observable = model.ObservableForProperty(m => m.CurrentCatalog.Value)
					.Subscribe(_ => {
						NotifyOfPropertyChange("CurrentItem");
						NotifyOfPropertyChange("CurrentCatalog");
						NotifyOfPropertyChange("CurrentCatalogName");
					});
				CleanCache();
				ActiveItem = model;
			}
			else if (!(ActiveItem is CatalogNameViewModel)) {
				observable.Dispose();
				CleanCache();

				var model = new CatalogNameViewModel(this);
				var composite = new CompositeDisposable {
					model
						.ObservableForProperty(m => m.CurrentItem.Value)
						.Subscribe(_ => NotifyOfPropertyChange("CurrentItem")),
					model
						.ObservableForProperty(m => m.CurrentCatalog)
						.Subscribe(_ => NotifyOfPropertyChange("CurrentCatalog")),
					model
						.ObservableForProperty(m => m.CurrentCatalogName.Value)
						.Subscribe(_ => NotifyOfPropertyChange("CurrentCatalogName"))
				};
				observable = composite;
				ActiveItem = model;
			}
		}

		private void CleanCache()
		{
			//Когда мы пересоздаем ActiveItem
			//выражение cal:Bind.ModelWithoutContext="{Binding}" DataContext="{Binding ParentModel}"
			//приводит к тому что в словарь Views добавляется CatalogPanel
			//что ведет к утечке памяти
			//при пересоздании нужно чистить словарь
			var toRemove = Views.Where(k => k.Value is CatalogPanel).ToArray();
			toRemove.Each(r => Views.Remove(r.Key));
		}

		public void ShowAwaited()
		{
			Shell.ShowAwaited();
		}

		public Catalog GuessCatalog()
		{
			if (CurrentCatalog != null)
				return CurrentCatalog;
			if (CurrentCatalogName != null && ActiveItem is CatalogNameViewModel)
				return (((CatalogNameViewModel)ActiveItem).Catalogs.Value ?? Enumerable.Empty<Catalog>()).FirstOrDefault();
			return null;
		}

		public IEnumerable<IResult> AddToAwaited()
		{
			if (!CanAddToAwaited.Value)
				yield break;
			var item = new AwaitedItem(GuessCatalog());
			string error;
			if (!item.TrySave(StatelessSession, out error)) {
				yield return new MessageResult(error, MessageResult.MessageType.Warning);
			}
			else {
				yield return new MessageResult("Выбранное наименование добавлено в список ожидаемых позиций.");
			}
		}

		public IEnumerable<IResult> ShowOrderHistory()
		{
			if (CurrentCatalog == null || Address == null)
				yield break;

			var addressId = Address.Id;
			var catalogId = CurrentCatalog.Id;
			var lines = StatelessSession.Query<SentOrderLine>()
				.Where(o => o.CatalogId == catalogId)
				.Where(o => o.Order.Address.Id == addressId)
				.OrderByDescending(o => o.Order.SentOn)
				.Fetch(l => l.Order)
				.ThenFetch(o => o.Price)
				.Take(20)
				.ToList();
			if (lines.Count > 0)
				Shell.Navigate(new HistoryOrdersViewModel(CurrentCatalog, null, lines));
			else
				yield return MessageResult.Warn("Нет истории заказов");
		}
	}
}