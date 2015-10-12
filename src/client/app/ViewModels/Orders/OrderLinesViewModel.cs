using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	//todo при удалении строки в предложениях должна удаляться строка и в заказах
	[DataContract]
	public class OrderLinesViewModel : BaseOfferViewModel, IPrintable
	{
		public OrderLinesViewModel()
		{
			DisplayName = "Сводный заказ";

			LinesCount = new NotifyValue<int>();
			OnlyWarning = new NotifyValue<bool>();
			CurrentLine = new NotifyValue<OrderLine>();
			Lines = new NotifyValue<ObservableCollection<OrderLine>>(new ObservableCollection<OrderLine>());
			SentLines = new NotifyValue<List<SentOrderLine>>(new List<SentOrderLine>());
			SelectedSentLine = new NotifyValue<SentOrderLine>();
			IsCurrentSelected = new NotifyValue<bool>(true);
			IsSentSelected = new NotifyValue<bool>();
			Begin = new NotifyValue<DateTime>(DateTime.Today);
			End = new NotifyValue<DateTime>(DateTime.Today);
			AddressSelector = new AddressSelector(this);

			CanDelete = CurrentLine.CombineLatest(IsCurrentSelected, (l, s) => l != null && s).ToValue();
			BeginEnabled = IsSentSelected.ToValue();
			EndEnabled = IsSentSelected.ToValue();

			IsCurrentSelected.Subscribe(_ => NotifyOfPropertyChange("CanPrint"));
			IsCurrentSelected.Subscribe(_ => NotifyOfPropertyChange("CanExport"));

			Sum = new NotifyValue<decimal>(() => {
				if (IsCurrentSelected)
					return Lines.Value.Sum(l => l.MixedSum);
				return SentLines.Value.Sum(l => l.MixedSum);
			}, SentLines, Lines, IsCurrentSelected);

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			QuickSearch = new QuickSearch<OrderLine>(UiScheduler,
				s => Lines.Value.FirstOrDefault(l => l.ProductSynonym.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0),
				CurrentLine);
			Editor = new Editor(OrderWarning, Manager, CurrentLine, Lines.Cast<IList>().ToValue());

			var currentLinesChanged = this.ObservableForProperty(m => m.CurrentLine.Value.Count)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));
			OnCloseDisposable.Add(Bus.RegisterMessageSource(currentLinesChanged));
			OnCloseDisposable.Add(currentLinesChanged.Subscribe(_ => Sum.Recalculate()));

			Settings.Subscribe(_ => CalculateOrderLine());

			if (Session != null)
				Prices = Session.Query<Price>().OrderBy(p => p.Name).ToList()
					.Select(p => new Selectable<Price>(p))
					.ToList();
			else
				Prices = new List<Selectable<Price>>();

			MatchedWaybills = new MatchedWaybills(this, SelectedSentLine, IsSentSelected);
			IsCurrentSelected
				.Select(v => v ? "Lines" : "SentLines")
				.Subscribe(ExcelExporter.ActiveProperty);

			Observable.Merge(IsCurrentSelected.Select(x => (object)x), Lines, SentLines, currentLinesChanged)
				.Select(_ => {
					if (IsCurrentSelected)
						return Lines.Value != null ? Lines.Value.Count : 0;
					return SentLines.Value != null ? SentLines.Value.Count : 0;
				})
				.Subscribe(LinesCount);
			IsLoading = new NotifyValue<bool>();
			IsCurrentSelected.Where(v => v)
				.Select(_ => false)
				.Subscribe(IsLoading);
		}

		public NotifyValue<bool> IsCurrentSelected { get; set ;}
		public NotifyValue<bool> IsSentSelected { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<bool> BeginEnabled { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<bool> EndEnabled { get; set; }

		public NotifyValue<bool> IsLoading { get; set; }
		public InlineEditWarning LinesOrderWarning { get; set; }
		public QuickSearch<OrderLine> QuickSearch { get; set; }
		public AddressSelector AddressSelector { get; set; }
		public ProductInfo ProductInfo { get; set; }
		public ProductInfo ProductInfo2 { get; set; }

		public List<Selectable<Price>> Prices { get; set; }

		public NotifyValue<bool> OnlyWarningVisible { get; set; }
		public NotifyValue<bool> OnlyWarning { get; set; }

		public NotifyValue<OrderLine> CurrentLine { get; set; }

		[Export]
		public NotifyValue<ObservableCollection<OrderLine>> Lines { get; set; }

		[Export]
		public NotifyValue<List<SentOrderLine>> SentLines { get; set; }

		public NotifyValue<SentOrderLine> SelectedSentLine { get; set; }

		public NotifyValue<int> LinesCount { get; set; }

		public NotifyValue<decimal> Sum { get; set; }

		public NotifyValue<bool> CanDelete { get; set; }

		public MatchedWaybills MatchedWaybills { get; set; }

		public bool CanPrint
		{
			get
			{
				if (IsCurrentSelected)
					return User.CanPrint<OrderLinesDocument, OrderLine>();
				return User.CanPrint<OrderLinesDocument, SentOrderLine>();
			}
		}

		public Editor Editor { get; set; }

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var binding = new CommandBinding(ProductInfo.ShowCatalogCommand,
				(sender, args) => {
					if (IsCurrentSelected)
						ProductInfo.ShowCatalog();
					else
						ProductInfo2.ShowCatalog();
				},
				(sender, args) => {
					args.CanExecute = IsCurrentSelected ? ProductInfo.CanShowCatalog : ProductInfo2.CanShowCatalog;
				});
			var binding1 = new CommandBinding(ProductInfo.ShowDescriptionCommand,
				(sender, args) => {
					if (IsCurrentSelected)
						ProductInfo.ShowDescription();
					else
						ProductInfo2.ShowDescription();
				},
				(sender, args) => {
					args.CanExecute = IsCurrentSelected ? ProductInfo.CanShowDescription : ProductInfo2.CanShowDescription;
				});
			var binding2 = new CommandBinding(ProductInfo.ShowMnnCommand,
				(sender, args) => {
					if (IsCurrentSelected)
						ProductInfo.ShowCatalogWithMnnFilter();
					else
						ProductInfo2.ShowCatalogWithMnnFilter();
				},
				(sender, args) => {
					args.CanExecute = IsCurrentSelected ? ProductInfo.CanShowCatalogWithMnnFilter : ProductInfo2.CanShowCatalogWithMnnFilter;
				});
			Attach(view as UIElement, new[] { binding, binding1, binding2 });
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			var beginValue = Shell.SessionContext.GetValueOrDefault(GetType().Name + ".Begin");
			if (beginValue is DateTime)
				Begin.Value = (DateTime)beginValue;
			var endValue = Shell.SessionContext.GetValueOrDefault(GetType().Name + ".End");
			if (endValue is DateTime)
				End.Value = (DateTime)endValue;
			var isSentSelectedValue = Shell.SessionContext.GetValueOrDefault(GetType().Name + ".IsSentSelected");
			if (isSentSelectedValue is bool)
				IsSentSelected.Value = (bool)isSentSelectedValue;

			OnlyWarningVisible = IsCurrentSelected.Select(v => v && User.IsPreprocessOrders).ToValue();
			ProductInfo = new ProductInfo(this, CurrentLine);
			ProductInfo2 = new ProductInfo(this, SelectedSentLine);
			AddressSelector.Init();
			AddressSelector.FilterChanged
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(OnlyWarning.Select(v => (object)v))
				.Where(_ => IsCurrentSelected && Session != null)
				.Select(_ => {
					var lines = AddressSelector.GetActiveFilter().SelectMany(o => o.ActiveOrders())
						.Where(x => Prices.Where(y => y.IsSelected).Select(y => y.Item.Id).Contains(x.Price.Id))
						.SelectMany(o => o.Lines)
						.Where(x => OnlyWarning ? x.SendResult != LineResultStatus.OK : true)
						.OrderBy(l => l.Id)
						.ToObservableCollection();
					lines.Each(l => {
						l.Order.CalculateStyle(Address);
						if (l.Order.IsAddressExists())
							l.CalculateRetailCost(Settings.Value.Markups, User, l.Order.Address);
					});
					return lines;
				})
				.Subscribe(Lines, CloseCancellation.Token);

			IsSentSelected.Where(v => v)
				.Select(v => (object)v)
				.Merge(Begin.Select(d => (object)d))
				.Merge(End.Select(d => (object)d))
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(AddressSelector.FilterChanged)
				.Do(_ => { IsLoading.Value = true; })
				//защита от множества запросов
				.Throttle(TimeSpan.FromMilliseconds(30), Scheduler)
				.Where(_ => IsSentSelected)
				.Select(_ => RxQuery(s => {
					var begin = Begin.Value;
					var end = End.Value.AddDays(1);
					var addressIds = AddressSelector.GetActiveFilter().Select(a => a.Id).ToArray();
					var query = s.Query<SentOrderLine>()
						.Fetch(l => l.Order)
						.ThenFetch(o => o.Address)
						.Fetch(o => o.Order)
						.ThenFetch(o => o.Price)
						.Where(l => l.Order.SentOn > begin && l.Order.SentOn < end)
						.Where(l => addressIds.Contains(l.Order.Address.Id));

					query = Util.Filter(query, l => l.Order.Price.Id, Prices);

					var lines = query.OrderBy(l => l.ProductSynonym)
						.ThenBy(l => l.ProductSynonym)
						.Take(1000)
						.ToList();
					if (Settings.Value.HighlightUnmatchedOrderLines) {
						var lookup = MatchedWaybills.GetLookUp(s, lines);
						lines.Each(l => l.Order.CalculateStyle(Address));
						lines.Each(l => l.Configure(User, lookup));
					}
					else {
						lines.Each(l => l.Order.CalculateStyle(Address));
						lines.Each(l => l.Configure(User));
					}
					return lines;
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.Do(_ => { IsLoading.Value = false; })
				.Subscribe(SentLines, CloseCancellation.Token);

			CurrentLine
				.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Subscribe(_ => { Update(); }, CloseCancellation.Token);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			AddressSelector.Deinit();
			Shell.SessionContext[GetType().Name + ".IsSentSelected"] = IsSentSelected.Value;
			Shell.SessionContext[GetType().Name + ".Begin"] = Begin.Value;
			Shell.SessionContext[GetType().Name + ".End"] = End.Value;
		}

		protected override void Query()
		{
			if (StatelessSession == null)
				return;
			if (CurrentLine.Value == null) {
				Offers.Value = new List<Offer>();
				return;
			}

			var productId = CurrentLine.Value.ProductId;
			Offers.Value = StatelessSession.Query<Offer>()
				.Fetch(o => o.Price)
				.Where(o => o.ProductId == productId)
				.ToList()
				.OrderBy(o => o.ResultCost)
				.ToList();
		}

		protected void CalculateOrderLine()
		{
			foreach (var line in Lines.Value) {
				if (line.Order.IsAddressExists())
					line.CalculateRetailCost(Settings.Value.Markups, User, line.Order.Address);
			}
		}

		public void EnterLine()
		{
			ProductInfo.ShowCatalog();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			Editor.Delete();
		}

		public override void TryClose()
		{
			Editor.Committed();
			base.TryClose();
		}

		public override void OfferCommitted()
		{
			base.OfferCommitted();
			//мы могли создать новую строку или удалить существующую
			//нужно обновить списко строк
			OnlyWarning.Refresh();
		}

		public PrintResult Print()
		{
			return new PrintResult(DisplayName, new OrderLinesDocument(this));
		}
	}
}