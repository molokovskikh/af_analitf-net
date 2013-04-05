using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
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
	public class OrderLinesViewModel : BaseOrderViewModel, IPrintable
	{
		private OrderLine currentLine;
		private List<MarkupConfig> markups;
		private Price currentPrice;
		private ObservableCollection<OrderLine> lines;
		private List<SentOrderLine> sentLines;

		private Editor editor;

		public OrderLinesViewModel()
		{
			OrderWarning = new InlineEditWarning(UiScheduler, Manager);
			QuickSearch = new QuickSearch<OrderLine>(UiScheduler,
				s => Lines.FirstOrDefault(l => l.ProductSynonym.ToLower().Contains(s)),
				l => CurrentLine = l);
			AddressSelector = new AddressSelector(Session, UiScheduler, this);
			editor = new Editor(OrderWarning, Manager);

			DisplayName = "Сводный заказ";
			markups = Session.Query<MarkupConfig>().ToList();

			Dep(m => m.Sum, m => m.Lines);
			Dep(m => m.CanDelete, m => m.CurrentLine, m => m.IsCurrentSelected);

			Dep(Update, m => m.CurrentPrice, m => m.AddressSelector.All.Value);

			this.ObservableForProperty(m => m.CurrentLine)
				.Subscribe(e => ProductInfo.CurrentOffer = e.Value);

			this.ObservableForProperty(m => m.CurrentLine)
				.Select(e => e.Value)
				.BindTo(editor, e => e.CurrentEdit);

			this.ObservableForProperty(m => m.Lines)
				.Select(e => e.Value)
				.BindTo(editor, e => e.Lines);

			editor.ObservableForProperty(e => e.CurrentEdit)
				.Select(e => e.Value)
				.BindTo(this, m => m.CurrentLine);

			var observable = this.ObservableForProperty(m => m.CurrentLine.Count)
				.Throttle(Consts.RefreshOrderStatTimeout, UiScheduler)
				.Select(e => new Stat(Address));
			OnCloseDisposable.Add(Bus.RegisterMessageSource(observable));
			OnCloseDisposable.Add(observable.Subscribe(_ => NotifyOfPropertyChange("Sum")));

			//пока устанавливаем значения не надо оповещать об изменения
			//все равно будет запрос когда форма активируется
			IsNotifying = false;
			var prices = Session.Query<Price>().OrderBy(p => p.Name);
			Prices = new[] { new Price {Name = Consts.AllPricesLabel} }.Concat(prices).ToList();
			CurrentPrice = Prices.FirstOrDefault();

			Begin = DateTime.Today;
			End = DateTime.Today;
			IsNotifying = true;
		}

		public InlineEditWarning OrderWarning { get; set; }
		public QuickSearch<OrderLine> QuickSearch { get; set; }
		public AddressSelector AddressSelector { get; set; }
		public ProductInfo ProductInfo { get; set; }

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);

			var commands = ProductInfo.Bindings;
			Attach(view, commands);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ProductInfo = new ProductInfo(StatelessSession, Manager, Shell);
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
		}

		public override void Update()
		{
			if (IsCurrentSelected) {
				var query = Session.Query<OrderLine>()
					.Where(l => !l.Order.Frozen);

				if (CurrentPrice != null && CurrentPrice.Id != null) {
					var priceId = CurrentPrice.Id;
					query = query.Where(l => l.Order.Price.Id == priceId);
				}

				if (!AddressSelector.All.Value) {
					var addressId = Address.Id;
					query = query.Where(l => l.Order.Address.Id == addressId);
				}
				else {
					var addresses = AddressSelector.Addresses.Where(i => i.IsSelected)
						.Select(i => i.Item.Id)
						.ToArray();
					query = query.Where(l => addresses.Contains(l.Order.Address.Id));
				}

				Lines = new ObservableCollection<OrderLine>(query
					.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProducerSynonym)
					.ToList());

				CalculateRetailCost();
			}
			else {
				var addressId = Address.Id;
				var query = StatelessSession.Query<SentOrderLine>()
					.Fetch(l => l.Order)
					.ThenFetch(o => o.Price)
					.Where(l => l.Order.SentOn > Begin && l.Order.SentOn < End.AddDays(1))
					.Where(l => l.Order.Address.Id == addressId);

				if (CurrentPrice != null && CurrentPrice.Id != null) {
					var priceId = CurrentPrice.Id;
					query = query.Where(l => l.Order.Price.Id == priceId);
				}

				SentLines = query.OrderBy(l => l.ProductSynonym)
					.ThenBy(l => l.ProductSynonym)
					.ToList();
			}
		}
		private void Dep(Action action, params Expression<Func<OrderLinesViewModel, object>>[] to)
		{
			to.Select(e => this.ObservableForProperty(e))
				.Merge()
				.Subscribe(e => action());
		}

		protected void Dep(Expression<Func<OrderLinesViewModel, object>> from, params Expression<Func<OrderLinesViewModel, object>>[] to)
		{
			var name = @from.GetProperty();
			to.Select(e => this.ObservableForProperty(e))
				.Merge()
				.Subscribe(e => NotifyOfPropertyChange(name));
		}

		protected void CalculateRetailCost()
		{
			foreach (var offer in Lines)
				offer.CalculateRetailCost(markups);
		}

		public List<Price> Prices { get; set; }

		public Price CurrentPrice
		{
			get { return currentPrice; }
			set
			{
				currentPrice = value;
				NotifyOfPropertyChange("CurrentPrice");
			}
		}

		[Export]
		public ObservableCollection<OrderLine> Lines
		{
			get { return lines; }
			set
			{
				lines = value;
				NotifyOfPropertyChange("Lines");
			}
		}

		[Export]
		public List<SentOrderLine> SentLines
		{
			get { return sentLines; }
			set
			{
				sentLines = value;
				NotifyOfPropertyChange("SentLines");
			}
		}

		public decimal Sum
		{
			get
			{
				return Lines.Sum(l => l.Sum);
			}
		}

		public OrderLine CurrentLine
		{
			get { return currentLine; }
			set
			{
				if (currentLine == value)
					return;

				currentLine = value;
				NotifyOfPropertyChange("CurrentLine");
			}
		}

		public void EnterLine()
		{
			ProductInfo.ShowCatalog();
		}

		public bool CanDelete
		{
			get { return CurrentLine != null && IsCurrentSelected; }
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

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			var doc = new OrderLinesDocument(this).BuildDocument();
			return new PrintResult(doc, DisplayName);
		}
	}
}