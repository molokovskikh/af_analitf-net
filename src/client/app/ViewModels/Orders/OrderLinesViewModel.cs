using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels.Orders
{
	[DataContract]
	public class OrderLinesViewModel : BaseOrderViewModel, IPrintable
	{
		private Editor editor;

		public OrderLinesViewModel()
		{
			DisplayName = "Сводный заказ";

			LinesCount = new NotifyValue<int>();
			OnlyWarning = new NotifyValue<bool>();
			CurrentLine = new NotifyValue<OrderLine>();
			Lines = new NotifyValue<ObservableCollection<OrderLine>>(new ObservableCollection<OrderLine>());
			SentLines = new NotifyValue<List<SentOrderLine>>(new List<SentOrderLine>());
			SelectedSentLine = new NotifyValue<SentOrderLine>();
			CanDelete = CurrentLine.CombineLatest(IsCurrentSelected, (l, s) => l != null && s).ToValue();

			Sum = new NotifyValue<decimal>(() => {
				if (IsCurrentSelected)
					return Lines.Value.Sum(l => l.MixedSum);
				return SentLines.Value.Sum(l => l.MixedSum);
			}, SentLines, Lines, IsCurrentSelected);

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			QuickSearch = new QuickSearch<OrderLine>(UiScheduler,
				s => Lines.Value.FirstOrDefault(l => l.ProductSynonym.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0),
				CurrentLine);
			AddressSelector = new AddressSelector(Session, this);
			editor = new Editor(OrderWarning, Manager, CurrentLine);

			Lines.CatchSubscribe(v => editor.Lines = v);
			var currentLinesChanged = this.ObservableForProperty(m => m.CurrentLine.Value.Count)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));
			OnCloseDisposable.Add(Bus.RegisterMessageSource(currentLinesChanged));
			OnCloseDisposable.Add(currentLinesChanged.Subscribe(_ => Sum.Recalculate()));

			Settings.Subscribe(_ => Calculate());

			if (Session != null)
				Prices = Session.Query<Price>().OrderBy(p => p.Name).ToList()
					.Select(p => new Selectable<Price>(p))
					.ToList();
			else
				Prices = new List<Selectable<Price>>();

			MatchedWaybills = new MatchedWaybills(this, SelectedSentLine, IsSentSelected);
			IsCurrentSelected
				.Select(v => v ? "Lines" : "SentLines")
				.Subscribe(excelExporter.ActiveProperty);

			Observable.Merge(IsCurrentSelected.Select(x => (object)x), Lines, SentLines, currentLinesChanged)
				.Select(_ => {
					if (IsCurrentSelected)
						return Lines.Value != null ? Lines.Value.Count : 0;
					return SentLines.Value != null ? SentLines.Value.Count : 0;
				})
				.Subscribe(LinesCount);
		}

		public InlineEditWarning OrderWarning { get; set; }
		public QuickSearch<OrderLine> QuickSearch { get; set; }
		public AddressSelector AddressSelector { get; set; }
		public ProductInfo ProductInfo { get; set; }

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

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var commands = ProductInfo.Bindings;
			Attach(view, commands);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			var isSentSelectedValue = Shell.SessionContext.GetValueOrDefault(GetType().Name + ".IsSentSelected");
			if (isSentSelectedValue is bool)
				IsSentSelected.Value = (bool)isSentSelectedValue;

			OnlyWarningVisible = IsCurrentSelected.Select(v => v && User.IsPreprocessOrders).ToValue();
			ProductInfo = new ProductInfo(this);
			CurrentLine.Cast<object>()
				.Merge(SelectedSentLine)
				.Merge(IsCurrentSelected.Select(v => (object)v))
				.Subscribe(_ => {
					if (IsCurrentSelected)
						ProductInfo.CurrentOffer = CurrentLine.Value;
					else
						ProductInfo.CurrentOffer = SelectedSentLine.Value;
				});
			AddressSelector.Init();
			AddressSelector.FilterChanged
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(OnlyWarning.Select(v => (object)v))
				.CatchSubscribe(_ => Update(), CloseCancellation);

			IsSentSelected.Where(v => v)
				.Select(v => (object)v)
				.Merge(Begin.Select(d => (object)d))
				.Merge(End.Select(d => (object)d))
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(AddressSelector.FilterChanged)
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
						.ToList();
					if (Settings.Value.HighlightUnmatchedOrderLines) {
						lines.Each(l => {
							var lookup = MatchedWaybills.GetLookUp(s, lines);
							l.Configure(User, lookup);
						});
					}
					else {
						lines.Each(l => l.Configure(User));
					}
					return lines;
				}))
				.Switch()
				.ObserveOn(UiScheduler)
				.Subscribe(SentLines, CloseCancellation.Token);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			AddressSelector.Deinit();
			Shell.SessionContext[GetType().Name + ".IsSentSelected"] = IsSentSelected.Value;
		}

		public override void Update()
		{
			if (!IsSuccessfulActivated)
				return;
			if (Session == null)
				return;
			if (IsCurrentSelected) {
				var query = Session.Query<OrderLine>()
					.Where(l => !l.Order.Frozen);

				if (OnlyWarning.Value)
					query = query.Where(l => l.SendResult != LineResultStatus.OK);

				query = Util.Filter(query, l => l.Order.Price.Id, Prices);

				var addresses = AddressSelector.GetActiveFilter()
					.Select(i => i.Id)
					.ToArray();
				query = query.Where(l => addresses.Contains(l.Order.Address.Id));

				Lines.Value = query
					.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProducerSynonym)
					.ToObservableCollection();

				Calculate();
			}
		}

		protected void Calculate()
		{
			foreach (var offer in Lines.Value)
				offer.CalculateRetailCost(Settings.Value.Markups, User);
		}

		public void EnterLine()
		{
			ProductInfo.ShowCatalog();
		}

		public void Delete()
		{
			if (!CanDelete)
				return;

			editor.Delete();
		}

		public void OfferUpdated()
		{
			editor.Updated();
		}

		public void OfferCommitted()
		{
			editor.Committed();
		}

		public PrintResult Print()
		{
			var doc = new OrderLinesDocument(this).Build();
			return new PrintResult(DisplayName, doc);
		}
	}
}