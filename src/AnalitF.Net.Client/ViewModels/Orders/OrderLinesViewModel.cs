using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Common.Tools;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
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
			CurrentPrice = new NotifyValue<Price>();
			CanDelete = new NotifyValue<bool>(
				() => CurrentLine.Value != null && IsCurrentSelected,
				CurrentLine, IsCurrentSelected);

			Sum = new NotifyValue<decimal>(() => {
				if (IsCurrentSelected)
					return Lines.Value.Sum(l => l.ResultSum);
				return SentLines.Value.Sum(l => l.ResultSum);
			}, SentLines, Lines, IsCurrentSelected);

			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			QuickSearch = new QuickSearch<OrderLine>(UiScheduler,
				s => Lines.Value.FirstOrDefault(l => l.ProductSynonym.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0),
				l => CurrentLine.Value = l);
			AddressSelector = new AddressSelector(Session, UiScheduler, this);
			editor = new Editor(OrderWarning, Manager);

			AddressSelector.All.Changed()
				.Merge(CurrentPrice.Changed())
				.Merge(OnlyWarning.Changed())
				.Subscribe(_ => Update());

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

			//пока устанавливаем значения не надо оповещать об изменения
			//все равно будет запрос когда форма активируется
			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice.Mute(Prices.FirstOrDefault());
		}

		public InlineEditWarning OrderWarning { get; set; }
		public QuickSearch<OrderLine> QuickSearch { get; set; }
		public AddressSelector AddressSelector { get; set; }
		public ProductInfo ProductInfo { get; set; }

		public List<Price> Prices { get; set; }
		public NotifyValue<Price> CurrentPrice { get; set; }

		public NotifyValue<bool> OnlyWarningVisible { get; set; }
		public NotifyValue<bool> OnlyWarning { get; set; }

		[Export]
		public NotifyValue<ObservableCollection<OrderLine>> Lines { get; set; }

		[Export]
		public NotifyValue<List<SentOrderLine>> SentLines { get; set; }

		public NotifyValue<SentOrderLine> SelectedSentLine { get; set; }

		public NotifyValue<decimal> Sum { get; set; }

		public NotifyValue<OrderLine> CurrentLine { get; set; }

		public NotifyValue<bool> CanDelete { get; set; }

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
		}

		public override void Update()
		{
			if (IsCurrentSelected) {
				var query = Session.Query<OrderLine>()
					.Where(l => !l.Order.Frozen);

				if (OnlyWarning.Value)
					query = query.Where(l => l.SendResult != LineResultStatus.OK);

				if (CurrentPrice.Value != null && CurrentPrice.Value.Id != null) {
					var priceId = CurrentPrice.Value.Id;
					query = query.Where(l => l.Order.Price.Id == priceId);
				}

				if (!AddressSelector.All.Value) {
					if (Address != null) {
						var addressId = Address.Id;
						query = query.Where(l => l.Order.Address.Id == addressId);
					}
				}
				else {
					var addresses = AddressSelector.Addresses.Where(i => i.IsSelected)
						.Select(i => i.Item.Id)
						.ToArray();
					query = query.Where(l => addresses.Contains(l.Order.Address.Id));
				}

				Lines.Value = new ObservableCollection<OrderLine>(query
					.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProducerSynonym)
					.ToList());

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

				if (CurrentPrice.Value != null && CurrentPrice.Value.Id != null) {
					var priceId = CurrentPrice.Value.Id;
					query = query.Where(l => l.Order.Price.Id == priceId);
				}

				SentLines.Value = query.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProductSynonym)
					.ToList();
			}
		}

		protected void Calculate()
		{
			foreach (var offer in Lines.Value)
				offer.CalculateRetailCost(Settings.Value.Markups);
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