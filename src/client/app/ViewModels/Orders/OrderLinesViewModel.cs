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

			OnlyWarning = new NotifyValue<bool>();
			CurrentLine = new NotifyValue<OrderLine>();
			Lines = new NotifyValue<ObservableCollection<OrderLine>>(new ObservableCollection<OrderLine>());
			SentLines = new NotifyValue<List<SentOrderLine>>(new List<SentOrderLine>());
			SelectedSentLine = new NotifyValue<SentOrderLine>();
			CanDelete = new NotifyValue<bool>(
				() => CurrentLine.Value != null && IsCurrentSelected,
				CurrentLine, IsCurrentSelected);

			Sum = new NotifyValue<decimal>(() => {
				if (IsCurrentSelected)
					return Lines.Value.Sum(l => l.MixedSum);
				return SentLines.Value.Sum(l => l.MixedSum);
			}, SentLines, Lines, IsCurrentSelected);

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			QuickSearch = new QuickSearch<OrderLine>(UiScheduler,
				s => Lines.Value.FirstOrDefault(l => l.ProductSynonym.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0),
				l => CurrentLine.Value = l);
			AddressSelector = new AddressSelector(Session, this);
			editor = new Editor(OrderWarning, Manager);

			this.ObservableForProperty(m => m.CurrentLine.Value)
				.Select(e => e.Value)
				.BindTo(editor, e => e.CurrentEdit);

			this.ObservableForProperty(m => m.Lines.Value)
				.Select(e => e.Value)
				.BindTo(editor, e => e.Lines);

			editor.ObservableForProperty(e => e.CurrentEdit)
				.Select(e => e.Value)
				.BindTo(this, m => m.CurrentLine.Value);

			var observable = this.ObservableForProperty(m => m.CurrentLine.Value.Count)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));
			OnCloseDisposable.Add(Bus.RegisterMessageSource(observable));
			OnCloseDisposable.Add(observable.Subscribe(_ => Sum.Recalculate()));

			Settings.Changed().Subscribe(_ => Calculate());

			Prices = Session.Query<Price>().OrderBy(p => p.Name).ToList()
				.Select(p => new Selectable<Price>(p))
				.ToList();

			MatchedWaybills = new MatchedWaybills(StatelessSession, SelectedSentLine, IsSentSelected, UiScheduler);
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

		public override bool CanExport
		{
			get
			{
				var property = IsCurrentSelected ? "Lines" : "SentLines";
				return excelExporter.CanExport && User.CanExport(this, property);
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

			OnlyWarningVisible = new NotifyValue<bool>(() => IsCurrentSelected && User.IsPreprocessOrders, IsCurrentSelected);
			ProductInfo = new ProductInfo(StatelessSession, Manager, Shell);
			CurrentLine.Changed()
				.Merge(SelectedSentLine.Changed())
				.Merge(IsCurrentSelected.Changed())
				.Subscribe(_ => {
					if (IsCurrentSelected)
						ProductInfo.CurrentOffer = CurrentLine.Value;
					else
						ProductInfo.CurrentOffer = SelectedSentLine.Value;
				});
			AddressSelector.Init();
			AddressSelector.FilterChanged
				.Merge(Prices.Select(p => p.Changed()).Merge().Throttle(Consts.FilterUpdateTimeout, UiScheduler))
				.Merge(OnlyWarning.Changed())
				.Subscribe(_ => Update(), CloseCancellation.Token);

			var isSentSelectedValue = Shell.SessionContext.GetValueOrDefault(GetType().Name + ".IsSentSelected");
			if (isSentSelectedValue is bool)
				IsSentSelected.Mute((bool)isSentSelectedValue);
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			AddressSelector.Deinit();
			Shell.SessionContext[GetType().Name + ".IsSentSelected"] = IsSentSelected.Value;
		}

		public override void Update()
		{
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
			else {
				var addressId = Address.Id;
				var begin = Begin.Value;
				var end = End.Value.AddDays(1);
				var query = StatelessSession.Query<SentOrderLine>()
					.Fetch(l => l.Order)
					.ThenFetch(o => o.Price)
					.Where(l => l.Order.SentOn > begin && l.Order.SentOn < end)
					.Where(l => l.Order.Address.Id == addressId);

				query = Util.Filter(query, l => l.Order.Price.Id, Prices);

				var lines = query.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProductSynonym)
					.ToList();
				lines.Each(l => l.Configure(User));
				SentLines.Value = lines;
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